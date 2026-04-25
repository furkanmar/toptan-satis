using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class ProductService(AppDbContext db, IWebHostEnvironment env, IHttpContextAccessor http)
{
    public async Task<List<ProductDto>> GetAllAsync(Guid? wholesalerId = null, Guid? categoryId = null)
    {
        var q = db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Include(p => p.Wholesaler)
            .Where(p => p.IsActive);

        if (wholesalerId.HasValue) q = q.Where(p => p.WholesalerId == wholesalerId);
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);

        return await q.Select(p => MapDto(p, http)).ToListAsync();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id)
    {
        var p = await db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Include(p => p.Wholesaler)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        return MapDto(p, http);
    }

    public async Task<ProductDto> CreateAsync(Guid wholesalerId, CreateProductDto dto)
    {
        var product = new Product
        {
            WholesalerId = wholesalerId,
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Unit = dto.Unit,
            MinOrderQty = dto.MinOrderQty,
            Stock = dto.Stock
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, Guid wholesalerId, UpdateProductDto dto)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        if (dto.Name != null) product.Name = dto.Name;
        if (dto.Description != null) product.Description = dto.Description;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.Unit.HasValue) product.Unit = dto.Unit.Value;
        if (dto.MinOrderQty.HasValue) product.MinOrderQty = dto.MinOrderQty.Value;
        if (dto.Stock.HasValue) product.Stock = dto.Stock.Value;
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;

        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task UploadImageAsync(Guid productId, Guid wholesalerId, IFormFile file)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        var ext = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var uploadPath = Path.Combine(env.WebRootPath, "uploads", fileName);

        await using var stream = File.Create(uploadPath);
        await file.CopyToAsync(stream);

        var isFirst = !await db.ProductImages.AnyAsync(i => i.ProductId == productId);
        db.ProductImages.Add(new ProductImage
        {
            ProductId = productId,
            FilePath = $"uploads/{fileName}",
            IsMain = isFirst
        });

        await db.SaveChangesAsync();
    }

    private static ProductDto MapDto(Product p, IHttpContextAccessor http)
    {
        var baseUrl = $"{http.HttpContext!.Request.Scheme}://{http.HttpContext.Request.Host}";
        return new ProductDto
        {
            Id = p.Id,
            WholesalerId = p.WholesalerId,
            WholesalerName = p.Wholesaler.CompanyName,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Unit = p.Unit.ToString(),
            MinOrderQty = p.MinOrderQty,
            Stock = p.Stock,
            IsActive = p.IsActive,
            Images = p.Images.Select(i => new ProductImageDto
            {
                Id = i.Id,
                Url = $"{baseUrl}/{i.FilePath}",
                IsMain = i.IsMain
            }).ToList()
        };
    }
}
