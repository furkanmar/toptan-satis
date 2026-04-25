using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;

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
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var wholesaler = await db.Wholesalers.FirstOrDefaultAsync(w => w.UserId == userId)
            ?? throw new KeyNotFoundException("Toptancı bulunamadı");

        if (dto.CompanyName != null) wholesaler.CompanyName = dto.CompanyName;
        if (dto.Phone != null) wholesaler.Phone = dto.Phone;
        if (dto.Address != null) wholesaler.Address = dto.Address;
        if (dto.Description != null) wholesaler.Description = dto.Description;

        await db.SaveChangesAsync();
        return Ok(wholesaler);
    }
}

public record UpdateWholesalerDto(string? CompanyName, string? Phone, string? Address, string? Description);
