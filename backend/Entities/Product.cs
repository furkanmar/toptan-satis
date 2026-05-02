namespace WholesaleApi.Entities;

[Auditable]
public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WholesalerId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public decimal Price { get; set; }          // Referans fiyat (en küçük birim)
    public int MinOrderQty { get; set; } = 1;
    public int Stock { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public int VatRate { get; set; } = 18;          // KDV oranı: 0, 1, 10, 18, 20
    public int? MinimumStockLevel { get; set; }     // null = alarm yok
    public decimal? MaxOrderAmount { get; set; }    // null = sınır yok; mağaza siparişi bu tutarı geçemez
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Wholesaler Wholesaler { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductUnitConfig> UnitConfigs { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
