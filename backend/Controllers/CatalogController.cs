using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

    // ─── GET: isim veya barkod ile arama ──────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] string? barcode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.CatalogItems
            .Include(c => c.Barcodes)
            .Include(c => c.Images)
            .Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(barcode))
            query = query.Where(c => c.Barcodes.Any(b => b.Barcode == barcode));
        else if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{q}%") ||
                                     (c.Brand != null && EF.Functions.ILike(c.Brand, $"%{q}%")));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => MapDto(c, BaseUrl))
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await db.CatalogItems
            .Include(c => c.Barcodes)
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (item is null) return NotFound();
        return Ok(MapDto(item, BaseUrl));
    }

    // ─── POST: yeni katalog ürünü ─────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin,Wholesaler")]
    public async Task<IActionResult> Create([FromBody] CreateCatalogItemDto dto)
    {
        var item = new CatalogItem
        {
            Name = dto.Name,
            Description = dto.Description,
            Brand = dto.Brand,
            Manufacturer = dto.Manufacturer,
            Unit = dto.Unit
        };

        db.CatalogItems.Add(item);

        foreach (var barcode in dto.Barcodes.Where(b => !string.IsNullOrWhiteSpace(b)))
            db.CatalogItemBarcodes.Add(new CatalogItemBarcode { CatalogItemId = item.Id, Barcode = barcode.Trim() });

        await db.SaveChangesAsync();

        var created = await db.CatalogItems
            .Include(c => c.Barcodes)
            .Include(c => c.Images)
            .FirstAsync(c => c.Id == item.Id);

        return Ok(MapDto(created, BaseUrl));
    }

    // ─── PUT: güncelle ────────────────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Wholesaler")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCatalogItemDto dto)
    {
        var item = await db.CatalogItems
            .Include(c => c.Barcodes)
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (item is null) return NotFound();

        if (dto.Name != null) item.Name = dto.Name;
        if (dto.Description != null) item.Description = dto.Description;
        if (dto.Brand != null) item.Brand = dto.Brand;
        if (dto.Manufacturer != null) item.Manufacturer = dto.Manufacturer;
        if (dto.Unit.HasValue) item.Unit = dto.Unit.Value;
        if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;

        await db.SaveChangesAsync();
        return Ok(MapDto(item, BaseUrl));
    }

    // ─── Barkod yönetimi ──────────────────────────────────────────────────────
    [HttpPost("{id:guid}/barcodes")]
    [Authorize(Roles = "Admin,Wholesaler")]
    public async Task<IActionResult> AddBarcode(Guid id, [FromBody] AddBarcodeDto dto)
    {
        var item = await db.CatalogItems.FindAsync(id);
        if (item is null) return NotFound();

        // Duplikat kontrolü
        if (await db.CatalogItemBarcodes.AnyAsync(b => b.Barcode == dto.Barcode))
            return BadRequest(new { error = "Bu barkod zaten kayıtlı" });

        db.CatalogItemBarcodes.Add(new CatalogItemBarcode
        {
            CatalogItemId = id,
            Barcode = dto.Barcode.Trim(),
            Note = dto.Note
        });
        await db.SaveChangesAsync();
        return Ok(new { message = "Barkod eklendi" });
    }

    [HttpDelete("{id:guid}/barcodes/{barcodeId:guid}")]
    [Authorize(Roles = "Admin,Wholesaler")]
    public async Task<IActionResult> DeleteBarcode(Guid id, Guid barcodeId)
    {
        var barcode = await db.CatalogItemBarcodes
            .FirstOrDefaultAsync(b => b.Id == barcodeId && b.CatalogItemId == id);
        if (barcode is null) return NotFound();

        db.CatalogItemBarcodes.Remove(barcode);
        await db.SaveChangesAsync();
        return Ok(new { message = "Barkod silindi" });
    }

    // ─── Görsel yönetimi ──────────────────────────────────────────────────────
    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "Admin,Wholesaler")]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
    {
        var item = await db.CatalogItems.FindAsync(id);
        if (item is null) return NotFound();

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
            return BadRequest(new { error = "Desteklenmeyen dosya formatı" });

        var fileName = $"cat_{Guid.NewGuid()}{ext}";
        var uploadPath = Path.Combine(env.WebRootPath, "uploads", fileName);
        await using var stream = File.Create(uploadPath);
        await file.CopyToAsync(stream);

        var isFirst = !await db.CatalogItemImages.AnyAsync(i => i.CatalogItemId == id);
        db.CatalogItemImages.Add(new CatalogItemImage
        {
            CatalogItemId = id,
            FilePath = $"uploads/{fileName}",
            IsMain = isFirst
        });
        await db.SaveChangesAsync();
        return Ok(new { message = "Görsel yüklendi" });
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [Authorize(Roles = "Admin,Wholesaler")]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId)
    {
        var image = await db.CatalogItemImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.CatalogItemId == id);
        if (image is null) return NotFound();

        var filePath = Path.Combine(env.WebRootPath, image.FilePath);
        if (File.Exists(filePath)) File.Delete(filePath);

        var wasMain = image.IsMain;
        db.CatalogItemImages.Remove(image);
        await db.SaveChangesAsync();

        if (wasMain)
        {
            var next = await db.CatalogItemImages.FirstOrDefaultAsync(i => i.CatalogItemId == id);
            if (next != null) { next.IsMain = true; await db.SaveChangesAsync(); }
        }
        return Ok(new { message = "Görsel silindi" });
    }

    [HttpPatch("{id:guid}/images/{imageId:guid}/set-main")]
    [Authorize(Roles = "Admin,Wholesaler")]
    public async Task<IActionResult> SetMainImage(Guid id, Guid imageId)
    {
        var images = await db.CatalogItemImages.Where(i => i.CatalogItemId == id).ToListAsync();
        if (!images.Any()) return NotFound();
        foreach (var img in images) img.IsMain = img.Id == imageId;
        await db.SaveChangesAsync();
        return Ok(new { message = "Ana görsel güncellendi" });
    }

    // ─── Mapper ───────────────────────────────────────────────────────────────
    private static CatalogItemDto MapDto(CatalogItem c, string baseUrl) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        Brand = c.Brand,
        Manufacturer = c.Manufacturer,
        Unit = c.Unit.ToString(),
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        Barcodes = c.Barcodes.Select(b => new BarcodeDto
        {
            Id = b.Id,
            Barcode = b.Barcode,
            Note = b.Note
        }).ToList(),
        Images = c.Images.OrderByDescending(i => i.IsMain).Select(i => new CatalogImageDto
        {
            Id = i.Id,
            Url = $"{baseUrl}/{i.FilePath}",
            IsMain = i.IsMain
        }).ToList()
    };
}
