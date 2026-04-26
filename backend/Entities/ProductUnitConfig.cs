namespace WholesaleApi.Entities;

/// <summary>
/// Ürünün bir ambalaj/birim tipini temsil eder.
/// Örnek: Coca-Cola → Adet (1 kutu, 18.50₺), Paket (6 kutu, 105₺), Koli (24 kutu, 400₺)
/// </summary>
public class ProductUnitConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string UnitType { get; set; } = null!;   // "Adet", "Paket", "Koli", "Kg", "Litre"
    public int ContentQty { get; set; } = 1;         // Kaç tane temel birim içeriyor (Koli=24, Paket=6)
    public decimal Price { get; set; }               // Bu birim tipinin fiyatı
    public int SortOrder { get; set; } = 0;          // Listeleme sırası (küçük → büyük)

    // Navigation
    public Product Product { get; set; } = null!;
    public ICollection<ProductBarcode> Barcodes { get; set; } = [];
}

/// <summary>
/// Bir birim tipine ait barkod. Bir birim tipinin birden fazla barkodu olabilir.
/// </summary>
public class ProductBarcode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UnitConfigId { get; set; }
    public string Barcode { get; set; } = null!;
    public string? Note { get; set; }

    // Navigation
    public ProductUnitConfig UnitConfig { get; set; } = null!;
}
