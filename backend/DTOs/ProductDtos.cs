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
    public List<ProductImageDto> Images { get; set; } = [];
}

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public bool IsMain { get; set; }
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
}
