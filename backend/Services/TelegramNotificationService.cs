using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using Polly.Retry;
using WholesaleApi.Data;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class TelegramNotificationService(
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
{
    private readonly string _botToken =
        Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
        ?? config["Telegram:BotToken"]
        ?? throw new InvalidOperationException("TELEGRAM_BOT_TOKEN eksik");

    // 3 deneme, exponential backoff: 2s → 4s → 8s
    private readonly ResiliencePipeline _retryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 2,               // ilk deneme + 2 retry = 3 toplam
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(2),
            UseJitter = true,
            OnRetry = args =>
            {
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    public async Task SendMessageAsync(string chatId, string text, string? parseMode = "HTML")
    {
        var logEntry = new NotificationLog
        {
            UserId = Guid.Empty,               // caller gerekirse override edebilir
            Type = NotificationType.OrderCreated,  // caller override eder — burada default
            Payload = JsonSerializer.Serialize(new { chatId, text }),
            Channel = NotificationChannel.Telegram,
            Status = NotificationStatus.Pending
        };

        string? lastError = null;

        try
        {
            await _retryPipeline.ExecuteAsync(async ct =>
            {
                logEntry.AttemptCount++;
                logEntry.LastAttemptAt = DateTime.UtcNow;

                var client = httpClientFactory.CreateClient("Telegram");
                var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

                var payload = new
                {
                    chat_id = chatId,
                    text,
                    parse_mode = parseMode
                };

                var response = await client.PostAsJsonAsync(url, payload, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    throw new HttpRequestException(
                        $"Telegram API hata: {(int)response.StatusCode} {body}");
                }
            });

            logEntry.Status = NotificationStatus.Sent;
            logger.LogInformation(
                "Telegram mesajı gönderildi: ChatId={ChatId} Attempt={Attempt}",
                chatId, logEntry.AttemptCount);
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            logEntry.Status = NotificationStatus.Failed;
            logEntry.ErrorMessage = ex.Message;

            logger.LogError(ex,
                "Telegram mesajı gönderilemedi: ChatId={ChatId} Attempt={Attempt}",
                chatId, logEntry.AttemptCount);
        }

        // NotificationLog kaydet
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.NotificationLogs.Add(logEntry);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NotificationLog kaydedilemedi");
        }

        // Gönderim başarısız olsa da caller'ı exception ile boğma —
        // bildirim gitmemesi kritik iş akışını durdurmamalı.
    }
}
