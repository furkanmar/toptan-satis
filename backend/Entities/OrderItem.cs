namespace WholesaleApi.Entities;

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }   // snapshot — sipariş anındaki birim fiyatı

    // Birim tipi snapshot (UnitConfig silinse bile kayıt korunur)
    public string UnitType { get; set; } = "Adet";
    public int ContentQty { get; set; } = 1; // kaç temel birim
    public int VatRate { get; set; } = 18;    // KDV snapshot

    // Navigation
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
