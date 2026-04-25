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
}

public record ToggleActiveDto(bool IsActive);
