namespace WholesaleApi.Entities;

public enum NotificationType
{
    OrderCreated,
    OrderConfirmed,
    OrderRejected,
    OrderCancelled,
    LowStock
}

public enum NotificationChannel { Telegram }

public enum NotificationStatus { Pending, Sent, Failed }

public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Payload { get; set; } = null!;         // JSON — mesaj metni / context
    public NotificationChannel Channel { get; set; } = NotificationChannel.Telegram;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int AttemptCount { get; set; } = 0;
    public DateTime? LastAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
