namespace WholesaleApi.Entities;

public enum UserRole { Admin, Wholesaler, Store }

[Auditable]
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? TelegramChatId { get; set; }         // Telegram bildirimleri için

    // Navigation
    public Wholesaler? Wholesaler { get; set; }
    public Store? Store { get; set; }
}
