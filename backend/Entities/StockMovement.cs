namespace WholesaleApi.Entities;

public enum MovementType
{
    InitialBalance,
    OrderConfirm,
    OrderCancel,
    OrderReject,
    ManualAdjustment,
    Return,
    ForceConfirmNegative
}

/// <summary>
/// Append-only stok hareket defteri. Hiçbir zaman UPDATE/DELETE yapılmaz.
/// </summary>
public class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public MovementType MovementType { get; set; }

    /// <summary>Base-unit bazında değişim miktarı (+/-)</summary>
    public int QuantityChange { get; set; }

    /// <summary>Hareket sonrası ürünün toplam stoğu</summary>
    public int BalanceAfter { get; set; }

    public Guid? OrderId { get; set; }

    /// <summary>Hareketi tetikleyen kullanıcı (User.Id)</summary>
    public Guid UserId { get; set; }

    /// <summary>Manuel düzeltme / özel açıklama</summary>
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Product Product { get; set; } = null!;
}
