using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WholesalersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await db.Wholesalers
            .Where(w => w.IsActive)
            .Select(w => new { w.Id, w.CompanyName, w.Phone, w.Address, w.Description })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var w = await db.Wholesalers
            .Where(w => w.Id == id && w.IsActive)
            .Select(w => new { w.Id, w.CompanyName, w.Phone, w.Address, w.Description })
            .FirstOrDefaultAsync();
        return w is null ? NotFound() : Ok(w);
    }

    [HttpPut("me")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateWholesalerDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var wholesaler = await db.Wholesalers.FirstOrDefaultAsync(w => w.UserId == userId)
            ?? throw new KeyNotFoundException("Toptancı bulunamadı");

        if (dto.CompanyName != null) wholesaler.CompanyName = dto.CompanyName;
        if (dto.Phone != null) wholesaler.Phone = dto.Phone;
        if (dto.Address != null) wholesaler.Address = dto.Address;
        if (dto.Description != null) wholesaler.Description = dto.Description;

        await db.SaveChangesAsync();
        return Ok(wholesaler);
    }

    // ─── Son stok hareketleri (dashboard widget) ──────────────────────────────
    /// GET /api/wholesalers/stock-movements?recent=10
    /// GET /api/wholesalers/stock-movements?page=1&pageSize=20

    [HttpGet("stock-movements")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] int? recent,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var wholesalerId = await db.Wholesalers
            .Where(w => w.UserId == userId)
            .Select(w => w.Id)
            .FirstOrDefaultAsync();

        if (wholesalerId == Guid.Empty) return NotFound();

        var productIds = await db.Products
            .Where(p => p.WholesalerId == wholesalerId)
            .Select(p => p.Id)
            .ToListAsync();

        var q = db.StockMovements
            .Include(sm => sm.Product)
            .Where(sm => productIds.Contains(sm.ProductId))
            .OrderByDescending(sm => sm.CreatedAt);

        if (recent.HasValue)
        {
            var items = await q.Take(recent.Value)
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
            return Ok(items);
        }

        // Paginated
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);
        var total = await q.CountAsync();
        var paged = await q
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

        return Ok(new StockMovementPageDto { Items = paged, Total = total, Page = page, PageSize = pageSize });
    }
}

public record UpdateWholesalerDto(string? CompanyName, string? Phone, string? Address, string? Description);
