using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class ProductService(AppDbContext db, IWebHostEnvironment env, IHttpContextAccessor http)
{
    // ─── Queries ──────────────────────────────────────────────────────────────

    public async Task<List<ProductDto>> GetAllAsync(Guid? wholesalerId = null, Guid? categoryId = null, bool includeInactive = false)
    {
        var q = db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Include(p => p.Wholesaler)
            .Include(p => p.UnitConfigs)
                .ThenInclude(u => u.Barcodes)
            .AsQueryable();

        if (!includeInactive) q = q.Where(p => p.IsActive);
        if (wholesalerId.HasValue) q = q.Where(p => p.WholesalerId == wholesalerId);
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);

        var list = await q.OrderBy(p => p.Name).ToListAsync();
        return list.Select(p => MapDto(p)).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id)
    {
        var p = await LoadProduct(id)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");
        return MapDto(p);
    }

    // ─── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<ProductDto> CreateAsync(Guid wholesalerId, CreateProductDto dto)
    {
        var product = new Product
        {
            WholesalerId = wholesalerId,
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            Description = dto.Description,
            Brand = dto.Brand,
            Manufacturer = dto.Manufacturer,
            Price = dto.Price,
            MinOrderQty = dto.MinOrderQty,
            Stock = dto.Stock,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Unit configs
        foreach (var uc in dto.UnitConfigs)
            await AddUnitConfigInternalAsync(product.Id, uc);

        // Eğer en az bir unit config varsa, Price'ı en küçük birimden senkronize et
        if (dto.UnitConfigs.Count > 0)
        {
            var minPrice = dto.UnitConfigs.Min(u => u.Price);
            product.Price = minPrice;
            await db.SaveChangesAsync();
        }

        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, Guid wholesalerId, UpdateProductDto dto)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        if (dto.Name != null) product.Name = dto.Name;
        if (dto.Description != null) product.Description = dto.Description;
        if (dto.Brand != null) product.Brand = dto.Brand;
        if (dto.Manufacturer != null) product.Manufacturer = dto.Manufacturer;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.MinOrderQty.HasValue) product.MinOrderQty = dto.MinOrderQty.Value;
        if (dto.Stock.HasValue) product.Stock = dto.Stock.Value;
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
        if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId.Value;

        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    // ─── Unit Config yönetimi ─────────────────────────────────────────────────

    public async Task<ProductUnitConfigDto> AddUnitConfigAsync(Guid productId, Guid wholesalerId, CreateUnitConfigDto dto)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        var config = await AddUnitConfigInternalAsync(productId, dto);
        return MapUnitConfigDto(config);
    }

    public async Task<ProductUnitConfigDto> UpdateUnitConfigAsync(Guid productId, Guid configId, Guid wholesalerId, UpdateUnitConfigDto dto)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        var config = await db.ProductUnitConfigs
            .Include(u => u.Barcodes)
            .FirstOrDefaultAsync(u => u.Id == configId && u.ProductId == productId)
            ?? throw new KeyNotFoundException("Birim tipi bulunamadı");

        if (dto.UnitType != null) config.UnitType = dto.UnitType;
        if (dto.ContentQty.HasValue) config.ContentQty = dto.ContentQty.Value;
        if (dto.Price.HasValue) config.Price = dto.Price.Value;
        if (dto.SortOrder.HasValue) config.SortOrder = dto.SortOrder.Value;

        await db.SaveChangesAsync();
        return MapUnitConfigDto(config);
    }

    public async Task DeleteUnitConfigAsync(Guid productId, Guid configId, Guid wholesalerId)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        var config = await db.ProductUnitConfigs.FirstOrDefaultAsync(u => u.Id == configId && u.ProductId == productId)
            ?? throw new KeyNotFoundException("Birim tipi bulunamadı");

        db.ProductUnitConfigs.Remove(config);
        await db.SaveChangesAsync();
    }

    // ─── Barcode yönetimi ─────────────────────────────────────────────────────

    public async Task<ProductBarcodeDto> AddBarcodeAsync(Guid productId, Guid configId, Guid wholesalerId, AddProductBarcodeDto dto)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        _ = await db.ProductUnitConfigs.FirstOrDefaultAsync(u => u.Id == configId && u.ProductId == productId)
            ?? throw new KeyNotFoundException("Birim tipi bulunamadı");

        var barcode = new ProductBarcode
        {
            UnitConfigId = configId,
            Barcode = dto.Barcode.Trim(),
            Note = dto.Note
        };
        db.ProductBarcodes.Add(barcode);
        await db.SaveChangesAsync();

        return new ProductBarcodeDto { Id = barcode.Id, Barcode = barcode.Barcode, Note = barcode.Note };
    }

    public async Task DeleteBarcodeAsync(Guid productId, Guid configId, Guid barcodeId, Guid wholesalerId)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        var barcode = await db.ProductBarcodes
            .FirstOrDefaultAsync(b => b.Id == barcodeId && b.UnitConfigId == configId)
            ?? throw new KeyNotFoundException("Barkod bulunamadı");

        db.ProductBarcodes.Remove(barcode);
        await db.SaveChangesAsync();
    }

    // ─── Image yönetimi ───────────────────────────────────────────────────────

    public async Task UploadImageAsync(Guid productId, Guid wholesalerId, IFormFile file)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
            throw new InvalidOperationException("Desteklenmeyen dosya formatı");

        var fileName = $"{Guid.NewGuid()}{ext}";
        var uploadPath = Path.Combine(env.WebRootPath, "uploads", fileName);
        await using var stream = File.Create(uploadPath);
        await file.CopyToAsync(stream);

        var isFirst = !await db.ProductImages.AnyAsync(i => i.ProductId == productId);
        db.ProductImages.Add(new ProductImage { ProductId = productId, FilePath = $"uploads/{fileName}", IsMain = isFirst });
        await db.SaveChangesAsync();
    }

    public async Task DeleteImageAsync(Guid productId, Guid imageId, Guid wholesalerId)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        var image = await db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId)
            ?? throw new KeyNotFoundException("Görsel bulunamadı");

        var wasMain = image.IsMain;
        var filePath = Path.Combine(env.WebRootPath, image.FilePath);
        if (File.Exists(filePath)) File.Delete(filePath);

        db.ProductImages.Remove(image);
        await db.SaveChangesAsync();

        if (wasMain)
        {
            var next = await db.ProductImages.FirstOrDefaultAsync(i => i.ProductId == productId);
            if (next != null) { next.IsMain = true; await db.SaveChangesAsync(); }
        }
    }

    public async Task SetMainImageAsync(Guid productId, Guid imageId, Guid wholesalerId)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        var images = await db.ProductImages.Where(i => i.ProductId == productId).ToListAsync();
        foreach (var img in images) img.IsMain = img.Id == imageId;
        await db.SaveChangesAsync();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ProductUnitConfig> AddUnitConfigInternalAsync(Guid productId, CreateUnitConfigDto dto)
    {
        var config = new ProductUnitConfig
        {
            ProductId = productId,
            UnitType = dto.UnitType,
            ContentQty = dto.ContentQty,
            Price = dto.Price,
            SortOrder = dto.SortOrder,
        };
        db.ProductUnitConfigs.Add(config);
        await db.SaveChangesAsync();

        foreach (var b in dto.Barcodes.Where(x => !string.IsNullOrWhiteSpace(x)))
            db.ProductBarcodes.Add(new ProductBarcode { UnitConfigId = config.Id, Barcode = b.Trim() });

        await db.SaveChangesAsync();

        config.Barcodes = await db.ProductBarcodes.Where(b => b.UnitConfigId == config.Id).ToListAsync();
        return config;
    }

    private async Task<Product?> LoadProduct(Guid id) =>
        await db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Include(p => p.Wholesaler)
            .Include(p => p.UnitConfigs.OrderBy(u => u.SortOrder))
                .ThenInclude(u => u.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == id);

    private string BaseUrl => $"{http.HttpContext!.Request.Scheme}://{http.HttpContext.Request.Host}";

    private ProductDto MapDto(Product p) => new()
    {
        Id = p.Id,
        WholesalerId = p.WholesalerId,
        WholesalerName = p.Wholesaler.CompanyName,
        CategoryId = p.CategoryId,
        CategoryName = p.Category.Name,
        Name = p.Name,
        Description = p.Description,
        Brand = p.Brand,
        Manufacturer = p.Manufacturer,
        Price = p.Price,
        MinOrderQty = p.MinOrderQty,
        Stock = p.Stock,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        Images = p.Images.OrderByDescending(i => i.IsMain).Select(i => new ProductImageDto
        {
            Id = i.Id,
            Url = $"{BaseUrl}/{i.FilePath}",
            IsMain = i.IsMain
        }).ToList(),
        UnitConfigs = p.UnitConfigs.OrderBy(u => u.SortOrder).Select(MapUnitConfigDto).ToList()
    };

    private static ProductUnitConfigDto MapUnitConfigDto(ProductUnitConfig u) => new()
    {
        Id = u.Id,
        UnitType = u.UnitType,
        ContentQty = u.ContentQty,
        Price = u.Price,
        SortOrder = u.SortOrder,
        Barcodes = u.Barcodes.Select(b => new ProductBarcodeDto { Id = b.Id, Barcode = b.Barcode, Note = b.Note }).ToList()
    };
}
