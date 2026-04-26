using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext db) : ControllerBase
{
    // ─── Kullanıcı yönetimi ───────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await db.Users
            .Include(u => u.Wholesaler)
            .Include(u => u.Store)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Email,
                Role = u.Role.ToString(),
                u.IsActive,
                u.CreatedAt,
                Wholesaler = u.Wholesaler == null ? null : new { u.Wholesaler.Id, u.Wholesaler.CompanyName },
                Store = u.Store == null ? null : new { u.Store.Id, u.Store.StoreName }
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPatch("users/{id:guid}")]
    public async Task<IActionResult> ToggleActive(Guid id, [FromBody] ToggleActiveDto dto)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.IsActive = dto.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { user.Id, user.IsActive });
    }

    // ─── Mağaza ↔ Toptancı ilişkileri ────────────────────────────────────────

    [HttpGet("store-wholesalers")]
    public async Task<IActionResult> GetStoreWholesalers()
    {
        var relations = await db.StoreWholesalers
            .Include(sw => sw.Store)
            .Include(sw => sw.Wholesaler)
            .OrderBy(sw => sw.Wholesaler.CompanyName)
            .ThenBy(sw => sw.Store.StoreName)
            .Select(sw => new
            {
                sw.StoreId,
                StoreName = sw.Store.StoreName,
                StorePhone = sw.Store.Phone,
                sw.WholesalerId,
                WholesalerName = sw.Wholesaler.CompanyName,
                sw.IsActive,
                sw.AssignedAt
            })
            .ToListAsync();

        return Ok(relations);
    }

    [HttpPost("store-wholesalers")]
    public async Task<IActionResult> CreateStoreWholesaler([FromBody] CreateStoreWholesalerDto dto)
    {
        var exists = await db.StoreWholesalers
            .AnyAsync(sw => sw.StoreId == dto.StoreId && sw.WholesalerId == dto.WholesalerId);
        if (exists) return BadRequest(new { error = "Bu ilişki zaten mevcut" });

        var storeExists = await db.Stores.AnyAsync(s => s.Id == dto.StoreId);
        var wholesalerExists = await db.Wholesalers.AnyAsync(w => w.Id == dto.WholesalerId);
        if (!storeExists || !wholesalerExists) return NotFound(new { error = "Mağaza veya toptancı bulunamadı" });

        db.StoreWholesalers.Add(new Entities.StoreWholesaler { StoreId = dto.StoreId, WholesalerId = dto.WholesalerId });
        await db.SaveChangesAsync();
        return Ok(new { message = "İlişki oluşturuldu" });
    }

    [HttpDelete("store-wholesalers/{storeId:guid}/{wholesalerId:guid}")]
    public async Task<IActionResult> DeleteStoreWholesaler(Guid storeId, Guid wholesalerId)
    {
        var rel = await db.StoreWholesalers
            .FirstOrDefaultAsync(sw => sw.StoreId == storeId && sw.WholesalerId == wholesalerId);
        if (rel is null) return NotFound();

        db.StoreWholesalers.Remove(rel);
        await db.SaveChangesAsync();
        return Ok(new { message = "İlişki silindi" });
    }
}

public record ToggleActiveDto(bool IsActive);
public record CreateStoreWholesalerDto(Guid StoreId, Guid WholesalerId);
