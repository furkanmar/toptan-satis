namespace WholesaleApi.Entities;

/// <summary>
/// Denetim kaydı — hem interceptor (otomatik) hem servis katmanı (explicit) tarafından yazılır.
/// Append-only: UPDATE/DELETE yapılmaz.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>İşlemi yapan kullanıcı. Sistem kaynaklıysa null.</summary>
    public Guid? UserId { get; set; }

    public string UserRole { get; set; } = "System";

    /// <summary>İş aksiyonu adı: "OrderConfirmed", "ProductPriceUpdate", "ProductUpdated" vb.</summary>
    public string Action { get; set; } = null!;

    /// <summary>Entity türü: "Order", "Product" vb.</summary>
    public string EntityType { get; set; } = null!;

    /// <summary>Entity'nin PK değeri (string olarak).</summary>
    public string EntityId { get; set; } = null!;

    /// <summary>
    /// Before/after değişiklikler veya aksiyon payload'ı (jsonb).
    /// Örnek: { "before": { "Price": 10 }, "after": { "Price": 12 } }
    /// </summary>
    public string? Changes { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
