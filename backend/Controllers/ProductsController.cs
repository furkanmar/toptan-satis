using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;
using WholesaleApi.Services;
using Microsoft.EntityFrameworkCore;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(ProductService productService, AppDbContext db) : ControllerBase
{
    // ─── Temel CRUD ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<List<ProductDto>> GetAll(
        [FromQuery] Guid? wholesalerId,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool includeInactive = false,
        [FromQuery] bool lowStock = false)
        => await productService.GetAllAsync(wholesalerId, categoryId, includeInactive, lowStock);

    [HttpGet("{id:guid}")]
    public async Task<ProductDto> GetById(Guid id)
        => await productService.GetByIdAsync(id);

    [HttpPost]
    [Authorize(Roles = "Wholesaler")]
    public async Task<ProductDto> Create([FromBody] CreateProductDto dto)
    {
        var wholesalerId = await GetWholesalerId();
        return await productService.CreateAsync(wholesalerId, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<ProductDto> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        var wholesalerId = await GetWholesalerId();
        return await productService.UpdateAsync(id, wholesalerId, dto);
    }

    // ─── Stok hareketleri ─────────────────────────────────────────────────────

    [HttpGet("{id:guid}/stock-movements")]
    [Authorize(Roles = "Wholesaler,Admin")]
    public async Task<StockMovementPageDto> GetStockMovements(
        Guid id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? types,   // comma-separated MovementType names
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var q = db.StockMovements
            .Include(sm => sm.Product)
            .Where(sm => sm.ProductId == id)
            .AsQueryable();

        if (from.HasValue) q = q.Where(sm => sm.CreatedAt >= from.Value);
        if (to.HasValue)   q = q.Where(sm => sm.CreatedAt <= to.Value);
        if (!string.IsNullOrEmpty(types))
        {
            var typeList = types.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => Enum.TryParse<MovementType>(t.Trim(), out var mt) ? mt : (MovementType?)null)
                .Where(t => t.HasValue).Select(t => t!.Value).ToList();
            if (typeList.Count > 0)
                q = q.Where(sm => typeList.Contains(sm.MovementType));
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(sm => sm.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sm => new StockMovementDto
            {
                Id = sm.Id,
                ProductId = sm.ProductId,
                ProductName = sm.Product.Name,
                MovementType = sm.MovementType.ToString(),
                QuantityChange = sm.QuantityChange,
                BalanceAfter = sm.BalanceAfter,
                OrderId = sm.OrderId,
                UserId = sm.UserId,
                Reason = sm.Reason,
                CreatedAt = sm.CreatedAt,
            })
            .ToListAsync();

        return new StockMovementPageDto { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    // ─── Görsel yönetimi ──────────────────────────────────────────────────────

    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
    {
        var wholesalerId = await GetWholesalerId();
        await productService.UploadImageAsync(id, wholesalerId, file);
        return Ok(new { message = "Görsel yüklendi" });
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId)
    {
        var wholesalerId = await GetWholesalerId();
        await productService.DeleteImageAsync(id, imageId, wholesalerId);
        return Ok(new { message = "Görsel silindi" });
    }

    [HttpPatch("{id:guid}/images/{imageId:guid}/set-main")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> SetMainImage(Guid id, Guid imageId)
    {
        var wholesalerId = await GetWholesalerId();
        await productService.SetMainImageAsync(id, imageId, wholesalerId);
        return Ok(new { message = "Ana görsel güncellendi" });
    }

    // ─── Unit Config yönetimi ─────────────────────────────────────────────────

    [HttpPost("{id:guid}/unit-configs")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<ProductUnitConfigDto> AddUnitConfig(Guid id, [FromBody] CreateUnitConfigDto dto)
    {
        var wholesalerId = await GetWholesalerId();
        return await productService.AddUnitConfigAsync(id, wholesalerId, dto);
    }

    [HttpPut("{id:guid}/unit-configs/{configId:guid}")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<ProductUnitConfigDto> UpdateUnitConfig(Guid id, Guid configId, [FromBody] UpdateUnitConfigDto dto)
    {
        var wholesalerId = await GetWholesalerId();
        return await productService.UpdateUnitConfigAsync(id, configId, wholesalerId, dto);
    }

    [HttpDelete("{id:guid}/unit-configs/{configId:guid}")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> DeleteUnitConfig(Guid id, Guid configId)
    {
        var wholesalerId = await GetWholesalerId();
        await productService.DeleteUnitConfigAsync(id, configId, wholesalerId);
        return Ok(new { message = "Birim tipi silindi" });
    }

    // ─── Barkod yönetimi ──────────────────────────────────────────────────────

    [HttpPost("{id:guid}/unit-configs/{configId:guid}/barcodes")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<ProductBarcodeDto> AddBarcode(Guid id, Guid configId, [FromBody] AddProductBarcodeDto dto)
    {
        var wholesalerId = await GetWholesalerId();
        return await productService.AddBarcodeAsync(id, configId, wholesalerId, dto);
    }

    [HttpDelete("{id:guid}/unit-configs/{configId:guid}/barcodes/{barcodeId:guid}")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> DeleteBarcode(Guid id, Guid configId, Guid barcodeId)
    {
        var wholesalerId = await GetWholesalerId();
        await productService.DeleteBarcodeAsync(id, configId, barcodeId, wholesalerId);
        return Ok(new { message = "Barkod silindi" });
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private async Task<Guid> GetWholesalerId()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var wholesaler = await db.Wholesalers.FirstOrDefaultAsync(w => w.UserId == userId)
            ?? throw new KeyNotFoundException("Toptancı profili bulunamadı");
        return wholesaler.Id;
    }
}
