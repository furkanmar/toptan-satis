using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;

namespace WholesaleApi.Services;

/// <summary>
/// Günlük arka plan servisi — ExpiresAt geçmiş IdempotencyKey kayıtlarını siler.
/// </summary>
public class IdempotencyCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<IdempotencyCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("IdempotencyCleanupService başlatıldı");

        while (!stoppingToken.IsCancellationRequested)
        {
            // İlk çalışmayı bir sonraki gece 02:00'ye planla
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(2);   // ertesi gün 02:00 UTC
            var delay = nextRun - now;

            logger.LogInformation(
                "IdempotencyCleanup: bir sonraki çalışma {NextRun:u} ({Minutes:F0} dakika sonra)",
                nextRun, delay.TotalMinutes);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await CleanupAsync(stoppingToken);
        }

        logger.LogInformation("IdempotencyCleanupService durdu");
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var deleted = await db.IdempotencyKeys
                .Where(ik => ik.ExpiresAt < DateTime.UtcNow)
                .ExecuteDeleteAsync(ct);

            logger.LogInformation(
                "IdempotencyCleanup tamamlandı: {Count} kayıt silindi", deleted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "IdempotencyCleanup sırasında hata oluştu");
        }
    }
}
