using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;
using WholesaleApi.Services.Storage;

namespace WholesaleApi.Services;

public class ProductService(
    AppDbContext db,
    IWebHostEnvironment env,
    IHttpContextAccessor http,
    IAuditService auditService,
    IFileStorageService fileStorage,
    LocalFileStorage localStorage)
{
    private const int MaxImageSize = 2000;
    private const int WebpQuality = 85;

    // Queries

    public async Task<List<ProductDto>> GetAllAsync(Guid? wholesalerId = null, Guid? categoryId = null, bool includeInactive = false, bool lowStock = false)
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
        if (lowStock) q = q.Where(p => p.MinimumStockLevel.HasValue && p.Stock <= p.MinimumStockLevel.Value);

        var list = await q.OrderBy(p => p.Name).ToListAsync();
        // Mağaza görünümünde (includeInactive=false) pasif unit config'ler gizlenir
        var tasks = list.Select(p => MapDtoAsync(p, showInactiveConfigs: includeInactive));
        return (await Task.WhenAll(tasks)).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id)
    {
        var p = await LoadProduct(id) ?? throw new KeyNotFoundException("Urun bulunamadi");
        return await MapDtoAsync(p);
    }

    // CRUD

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
            VatRate = dto.VatRate,
            MinOrderQty = dto.MinOrderQty,
            Stock = dto.Stock,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        foreach (var uc in dto.UnitConfigs)
            await AddUnitConfigInternalAsync(product.Id, uc);

        if (dto.UnitConfigs.Count > 0)
        {
            product.Price = dto.UnitConfigs.Min(u => u.Price);
            await db.SaveChangesAsync();
        }

        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, Guid wholesalerId, UpdateProductDto dto)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        var oldPrice = product.Price;
        var oldVatRate = product.VatRate;

        if (dto.Name != null) product.Name = dto.Name;
        if (dto.Description != null) product.Description = dto.Description;
        if (dto.Brand != null) product.Brand = dto.Brand;
        if (dto.Manufacturer != null) product.Manufacturer = dto.Manufacturer;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.MinOrderQty.HasValue) product.MinOrderQty = dto.MinOrderQty.Value;
        if (dto.Stock.HasValue) product.Stock = dto.Stock.Value;
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
        if (dto.VatRate.HasValue) product.VatRate = dto.VatRate.Value;
        if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId.Value;
        if (dto.MinimumStockLevel.HasValue)
            product.MinimumStockLevel = dto.MinimumStockLevel.Value < 0 ? null : dto.MinimumStockLevel.Value;
        if (dto.MaxOrderAmount.HasValue)
            product.MaxOrderAmount = dto.MaxOrderAmount.Value <= 0 ? null : dto.MaxOrderAmount.Value;

        if (dto.Price.HasValue && dto.Price.Value != oldPrice)
        {
            auditService.LogAction(
                CurrentUserId, CurrentRole, "ProductPriceUpdate", "Product", id.ToString(),
                new { OldPrice = oldPrice, NewPrice = dto.Price.Value, OldVatRate = oldVatRate, NewVatRate = product.VatRate },
                CurrentIp);
        }

        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    // Unit Config

    public async Task<ProductUnitConfigDto> AddUnitConfigAsync(Guid productId, Guid wholesalerId, CreateUnitConfigDto dto)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        var config = await AddUnitConfigInternalAsync(productId, dto);
        return MapUnitConfigDto(config);
    }

    public async Task<ProductUnitConfigDto> UpdateUnitConfigAsync(Guid productId, Guid configId, Guid wholesalerId, UpdateUnitConfigDto dto)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        var config = await db.ProductUnitConfigs
            .Include(u => u.Barcodes)
            .FirstOrDefaultAsync(u => u.Id == configId && u.ProductId == productId)
            ?? throw new KeyNotFoundException("Birim tipi bulunamadi");

        if (dto.UnitType != null) config.UnitType = dto.UnitType;
        if (dto.ContentQty.HasValue) config.ContentQty = dto.ContentQty.Value;
        if (dto.Price.HasValue) config.Price = dto.Price.Value;
        if (dto.SortOrder.HasValue) config.SortOrder = dto.SortOrder.Value;
        if (dto.IsActive.HasValue) config.IsActive = dto.IsActive.Value;

        await db.SaveChangesAsync();
        return MapUnitConfigDto(config);
    }

    public async Task DeleteUnitConfigAsync(Guid productId, Guid configId, Guid wholesalerId)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        var config = await db.ProductUnitConfigs.FirstOrDefaultAsync(u => u.Id == configId && u.ProductId == productId)
            ?? throw new KeyNotFoundException("Birim tipi bulunamadi");

        db.ProductUnitConfigs.Remove(config);
        await db.SaveChangesAsync();
    }

    // Barcode

    public async Task<ProductBarcodeDto> AddBarcodeAsync(Guid productId, Guid configId, Guid wholesalerId, AddProductBarcodeDto dto)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        _ = await db.ProductUnitConfigs.FirstOrDefaultAsync(u => u.Id == configId && u.ProductId == productId)
            ?? throw new KeyNotFoundException("Birim tipi bulunamadi");

        var barcode = new ProductBarcode { UnitConfigId = configId, Barcode = dto.Barcode.Trim(), Note = dto.Note };
        db.ProductBarcodes.Add(barcode);
        await db.SaveChangesAsync();

        return new ProductBarcodeDto { Id = barcode.Id, Barcode = barcode.Barcode, Note = barcode.Note };
    }

    public async Task DeleteBarcodeAsync(Guid productId, Guid configId, Guid barcodeId, Guid wholesalerId)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        var barcode = await db.ProductBarcodes
            .FirstOrDefaultAsync(b => b.Id == barcodeId && b.UnitConfigId == configId)
            ?? throw new KeyNotFoundException("Barkod bulunamadi");

        db.ProductBarcodes.Remove(barcode);
        await db.SaveChangesAsync();
    }

    // Image

    public async Task UploadImageAsync(Guid productId, Guid wholesalerId, IFormFile file)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
            throw new InvalidOperationException("Desteklenmeyen dosya formati");

        // ImageSharp: resize + WebP
        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream);

        if (image.Width > MaxImageSize || image.Height > MaxImageSize)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxImageSize, MaxImageSize)
            }));
        }

        var ms = new MemoryStream();
        await image.SaveAsync(ms, new WebpEncoder { Quality = WebpQuality });
        ms.Position = 0;

        // R2'ye yukle
        var storageKey = $"products/{productId}/{Guid.NewGuid()}.webp";
        await fileStorage.UploadAsync(ms, storageKey, "image/webp");

        var isFirst = !await db.ProductImages.AnyAsync(i => i.ProductId == productId);
        db.ProductImages.Add(new ProductImage { ProductId = productId, FilePath = storageKey, IsMain = isFirst });
        await db.SaveChangesAsync();
    }

    public async Task DeleteImageAsync(Guid productId, Guid imageId, Guid wholesalerId)
    {
        _ = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        var image = await db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId)
            ?? throw new KeyNotFoundException("Gorsel bulunamadi");

        var wasMain = image.IsMain;

        if (fileStorage.Owns(image.FilePath))
            await fileStorage.DeleteAsync(image.FilePath);
        else if (localStorage.Owns(image.FilePath))
            await localStorage.DeleteAsync(image.FilePath);

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
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        var images = await db.ProductImages.Where(i => i.ProductId == productId).ToListAsync();
        foreach (var img in images) img.IsMain = img.Id == imageId;
        await db.SaveChangesAsync();
    }

    // Helpers

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

    private Guid? CurrentUserId =>
        Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private string CurrentRole =>
        http.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? "System";

    private string? CurrentIp =>
        http.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private async Task<ProductDto> MapDtoAsync(Product p, bool showInactiveConfigs = true)
    {
        var images = new List<ProductImageDto>();
        foreach (var i in p.Images.OrderByDescending(x => x.IsMain))
        {
            string url;
            if (fileStorage.Owns(i.FilePath))
                url = await fileStorage.GetPresignedUrlAsync(i.FilePath, TimeSpan.FromHours(1));
            else
                url = $"{BaseUrl}/{i.FilePath}";

            images.Add(new ProductImageDto { Id = i.Id, Url = url, IsMain = i.IsMain });
        }

        return new ProductDto
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
            VatRate = p.VatRate,
            MinOrderQty = p.MinOrderQty,
            Stock = p.Stock,
            IsActive = p.IsActive,
            MinimumStockLevel = p.MinimumStockLevel,
            MaxOrderAmount = p.MaxOrderAmount,
            CreatedAt = p.CreatedAt,
            Images = images,
            UnitConfigs = p.UnitConfigs
                .OrderBy(u => u.SortOrder)
                .Where(u => showInactiveConfigs || u.IsActive)
                .Select(MapUnitConfigDto).ToList()
        };
    }

    private static ProductUnitConfigDto MapUnitConfigDto(ProductUnitConfig u) => new()
    {
        Id = u.Id,
        UnitType = u.UnitType,
        ContentQty = u.ContentQty,
        Price = u.Price,
        SortOrder = u.SortOrder,
        IsActive = u.IsActive,
        Barcodes = u.Barcodes.Select(b => new ProductBarcodeDto { Id = b.Id, Barcode = b.Barcode, Note = b.Note }).ToList()
    };
}
