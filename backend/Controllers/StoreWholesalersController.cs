using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.Entities;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/store-wholesalers")]
[Authorize]
public class StoreWholesalersController(AppDbContext db) : ControllerBase
{
    // Admin — mağazaya toptancı ata
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Assign([FromBody] AssignDto dto)
    {
        var exists = await db.StoreWholesalers
            .AnyAsync(sw => sw.StoreId == dto.StoreId && sw.WholesalerId == dto.WholesalerId);

        if (exists)
        {
            // Zaten var — aktif et
            var sw = await db.StoreWholesalers
                .FirstAsync(sw => sw.StoreId == dto.StoreId && sw.WholesalerId == dto.WholesalerId);
            sw.IsActive = true;
            await db.SaveChangesAsync();
            return Ok(sw);
        }

        var newSw = new StoreWholesaler { StoreId = dto.StoreId, WholesalerId = dto.WholesalerId };
        db.StoreWholesalers.Add(newSw);
        await db.SaveChangesAsync();
        return Ok(newSw);
    }

    // Admin — atamayı kaldır
    [HttpDelete]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Remove([FromBody] AssignDto dto)
    {
        var sw = await db.StoreWholesalers
            .FirstOrDefaultAsync(sw => sw.StoreId == dto.StoreId && sw.WholesalerId == dto.WholesalerId);
        if (sw is null) return NotFound();
        sw.IsActive = false;
        await db.SaveChangesAsync();
        return Ok();
    }

    // Admin — bir mağazanın toptancılarını listele
    [HttpGet("store/{storeId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetForStore(Guid storeId)
    {
        var list = await db.StoreWholesalers
            .Include(sw => sw.Wholesaler)
            .Where(sw => sw.StoreId == storeId)
            .Select(sw => new { sw.WholesalerId, sw.Wholesaler.CompanyName, sw.IsActive, sw.AssignedAt })
            .ToListAsync();
        return Ok(list);
    }

    // Mağaza — kendi atanmış toptancılarını getir (login sonrası seçim)
    [HttpGet("my")]
    [Authorize(Roles = "Store")]
    public async Task<IActionResult> MyWholesalers()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var store = await db.Stores.FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException("Mağaza bulunamadı");

        var list = await db.StoreWholesalers
            .Include(sw => sw.Wholesaler)
            .Where(sw => sw.StoreId == store.Id && sw.IsActive)
            .Select(sw => new
            {
                sw.WholesalerId,
                sw.Wholesaler.CompanyName,
                sw.Wholesaler.Phone,
                sw.Wholesaler.Address,
                sw.Wholesaler.Description,
                sw.AssignedAt
            })
            .ToListAsync();

        return Ok(list);
    }
}

public record AssignDto(Guid StoreId, Guid WholesalerId);
