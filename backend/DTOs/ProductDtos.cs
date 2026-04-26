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
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public decimal Price { get; set; }          // Referans fiyat
    public int MinOrderQty { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProductImageDto> Images { get; set; } = [];
    public List<ProductUnitConfigDto> UnitConfigs { get; set; } = [];
}

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public bool IsMain { get; set; }
}

public class ProductUnitConfigDto
{
    public Guid Id { get; set; }
    public string UnitType { get; set; } = null!;
    public int ContentQty { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
    public List<ProductBarcodeDto> Barcodes { get; set; } = [];
}

public class ProductBarcodeDto
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = null!;
    public string? Note { get; set; }
}

// ─── Create / Update ──────────────────────────────────────────────────────────

public class CreateProductDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public decimal Price { get; set; }
    public int MinOrderQty { get; set; } = 1;
    public int Stock { get; set; } = 0;
    public List<CreateUnitConfigDto> UnitConfigs { get; set; } = [];
}

public class UpdateProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public decimal? Price { get; set; }
    public int? MinOrderQty { get; set; }
    public int? Stock { get; set; }
    public bool? IsActive { get; set; }
    public Guid? CategoryId { get; set; }
}

// ─── Unit Config yönetimi ─────────────────────────────────────────────────────

public class CreateUnitConfigDto
{
    public string UnitType { get; set; } = null!;
    public int ContentQty { get; set; } = 1;
    public decimal Price { get; set; }
    public int SortOrder { get; set; } = 0;
    public List<string> Barcodes { get; set; } = [];  // İlk barkodlar (virgülsüz)
}

public class UpdateUnitConfigDto
{
    public string? UnitType { get; set; }
    public int? ContentQty { get; set; }
    public decimal? Price { get; set; }
    public int? SortOrder { get; set; }
}

public class AddProductBarcodeDto
{
    public string Barcode { get; set; } = null!;
    public string? Note { get; set; }
}
