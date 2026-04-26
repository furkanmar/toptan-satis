using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.Entities;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? wholesalerId)
    {
        var cats = await db.Categories
            .Where(c => c.WholesalerId == wholesalerId)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return Ok(cats);
    }

    [HttpPost]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var wholesalerId = await GetWholesalerId();
        var slug = dto.Name.ToLower().Replace(" ", "-");

        if (await db.Categories.AnyAsync(c => c.Slug == slug && c.WholesalerId == wholesalerId))
            return BadRequest(new { error = "Bu kategori zaten var" });

        var cat = new Category { Name = dto.Name, Slug = slug, WholesalerId = wholesalerId };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return Ok(new { cat.Id, cat.Name, cat.Slug, cat.WholesalerId });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var wholesalerId = await GetWholesalerId();
        var cat = await db.Categories.FindAsync(id);
        if (cat is null) return NotFound();
        if (cat.WholesalerId != wholesalerId) return Forbid();

        // Kategoride ürün varsa "Diğer" kategorisine taşı
        var products = await db.Products.Where(p => p.CategoryId == id).ToListAsync();
        if (products.Count > 0)
        {
            var diger = await db.Categories
                .FirstOrDefaultAsync(c => c.WholesalerId == wholesalerId && c.Slug == "diger");

            if (diger is null)
            {
                diger = new Category { Name = "Diğer", Slug = "diger", WholesalerId = wholesalerId };
                db.Categories.Add(diger);
                await db.SaveChangesAsync();
            }

            foreach (var p in products) p.CategoryId = diger.Id;
            await db.SaveChangesAsync();
        }

        db.Categories.Remove(cat);
        await db.SaveChangesAsync();
        return Ok(new { reassigned = products.Count });
    }

    private async Task<Guid> GetWholesalerId()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var wholesaler = await db.Wholesalers.FirstOrDefaultAsync(w => w.UserId == userId)
            ?? throw new KeyNotFoundException("Toptancı profili bulunamadı");
        return wholesaler.Id;
    }
}

public record CreateCategoryDto(string Name);
