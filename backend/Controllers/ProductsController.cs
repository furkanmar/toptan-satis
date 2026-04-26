using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
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
        [FromQuery] bool includeInactive = false)
        => await productService.GetAllAsync(wholesalerId, categoryId, includeInactive);

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
