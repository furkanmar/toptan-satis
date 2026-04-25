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
    [HttpGet]
    public async Task<List<ProductDto>> GetAll(
        [FromQuery] Guid? wholesalerId,
        [FromQuery] Guid? categoryId)
        => await productService.GetAllAsync(wholesalerId, categoryId);

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

    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
    {
        var wholesalerId = await GetWholesalerId();
        await productService.UploadImageAsync(id, wholesalerId, file);
        return Ok(new { message = "Görsel yüklendi" });
    }

    private async Task<Guid> GetWholesalerId()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var wholesaler = await db.Wholesalers.FirstOrDefaultAsync(w => w.UserId == userId)
            ?? throw new KeyNotFoundException("Toptancı profili bulunamadı");
        return wholesaler.Id;
    }
}
