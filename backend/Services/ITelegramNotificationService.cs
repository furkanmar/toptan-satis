namespace WholesaleApi.Services;

public interface ITelegramNotificationService
{
    /// <summary>
    /// Telegram mesajı gönderir. Başarısız olursa NotificationLog'a hata kaydeder.
    /// </summary>
    Task SendMessageAsync(string chatId, string text, string? parseMode = "HTML");
}
