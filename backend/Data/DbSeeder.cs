using Microsoft.EntityFrameworkCore;
using WholesaleApi.Entities;

namespace WholesaleApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // ─── Kategoriler ─────────────────────────────────────────────────────
        if (!await db.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category { Name = "Gıda",           Slug = "gida" },
                new Category { Name = "İçecek",         Slug = "icecek" },
                new Category { Name = "Temizlik",        Slug = "temizlik" },
                new Category { Name = "Kişisel Bakım",   Slug = "kisisel-bakim" },
                new Category { Name = "Kırtasiye",       Slug = "kirtasiye" },
                new Category { Name = "Elektronik",      Slug = "elektronik" },
                new Category { Name = "Tekstil",         Slug = "tekstil" },
                new Category { Name = "Diğer",           Slug = "diger" },
            };
            db.Categories.AddRange(categories);
            await db.SaveChangesAsync();
        }

        // ─── Admin kullanıcı ─────────────────────────────────────────────────
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var admin = new User
            {
                Email = "admin@marifoglu.trade",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Admin
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
