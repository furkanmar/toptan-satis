using WholesaleApi.Entities;

namespace WholesaleApi.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public Guid WholesalerId { get; set; }
    public string WholesalerName { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Unit { get; set; } = null!;
    public int MinOrderQty { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProductImageDto> Images { get; set; } = [];

    // Katalog bilgileri (bağlıysa)
    public Guid? CatalogItemId { get; set; }
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public List<BarcodeDto> Barcodes { get; set; } = [];
}

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public bool IsMain { get; set; }
}

public class BarcodeDto
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = null!;
    public string? Note { get; set; }
}

public class CreateProductDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public ProductUnit Unit { get; set; } = ProductUnit.Adet;
    public int MinOrderQty { get; set; } = 1;
    public int Stock { get; set; } = 0;
    public Guid? CatalogItemId { get; set; }  // Opsiyonel katalog bağlantısı
}

public class UpdateProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public ProductUnit? Unit { get; set; }
    public int? MinOrderQty { get; set; }
    public int? Stock { get; set; }
    public bool? IsActive { get; set; }
    public Guid? CategoryId { get; set; }      // Kategori değiştirme
    public Guid? CatalogItemId { get; set; }   // Katalog bağlantısı güncelleme
}

// ─── Catalog DTOs ─────────────────────────────────────────────────────────────

public class CatalogItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public string Unit { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BarcodeDto> Barcodes { get; set; } = [];
    public List<CatalogImageDto> Images { get; set; } = [];
    public int ProductCount { get; set; }  // Kaç toptancı bu ürünü satıyor
}

public class CatalogImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public bool IsMain { get; set; }
}

public class CreateCatalogItemDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public ProductUnit Unit { get; set; } = ProductUnit.Adet;
    public List<string> Barcodes { get; set; } = [];  // İlk barkodlar
}

public class UpdateCatalogItemDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public ProductUnit? Unit { get; set; }
    public bool? IsActive { get; set; }
}

public class AddBarcodeDto
{
    public string Barcode { get; set; } = null!;
    public string? Note { get; set; }
}
