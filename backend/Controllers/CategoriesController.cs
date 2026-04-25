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
    public async Task<IActionResult> GetAll()
    {
        var cats = await db.Categories.OrderBy(c => c.Name).ToListAsync();
        return Ok(cats);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var slug = dto.Name.ToLower().Replace(" ", "-");
        if (await db.Categories.AnyAsync(c => c.Slug == slug))
            throw new InvalidOperationException("Bu kategori zaten var");

        var cat = new Category { Name = dto.Name, Slug = slug };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return Ok(cat);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var cat = await db.Categories.FindAsync(id);
        if (cat is null) return NotFound();
        if (await db.Products.AnyAsync(p => p.CategoryId == id))
            throw new InvalidOperationException("Bu kategoride ürün var, önce ürünleri taşı");
        db.Categories.Remove(cat);
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record CreateCategoryDto(string Name);
