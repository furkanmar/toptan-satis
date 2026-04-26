namespace WholesaleApi.Entities;

public class CatalogItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Brand { get; set; }           // Marka: Coca-Cola, Ülker…
    public string? Manufacturer { get; set; }    // Menşei / Üretici
    public ProductUnit Unit { get; set; } = ProductUnit.Adet;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CatalogItemBarcode> Barcodes { get; set; } = [];
    public ICollection<CatalogItemImage> Images { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}

public class CatalogItemBarcode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CatalogItemId { get; set; }
    public string Barcode { get; set; } = null!;  // EAN-13, EAN-8, QR vb.
    public string? Note { get; set; }             // "eski barkod", "kutu barkodu"…

    // Navigation
    public CatalogItem CatalogItem { get; set; } = null!;
}

public class CatalogItemImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CatalogItemId { get; set; }
    public string FilePath { get; set; } = null!;  // relative: uploads/xxx.jpg
    public bool IsMain { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CatalogItem CatalogItem { get; set; } = null!;
}
