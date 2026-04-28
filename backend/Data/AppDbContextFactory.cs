using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WholesaleApi.Data;

/// <summary>
/// EF Core design-time factory — sadece "dotnet ef migrations" komutları için kullanılır.
/// Program.cs'teki env variable kontrolleri atlanır; bağlantı string'i doğrudan okunur.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // .env veya kullanıcı secrets'tan bağlantı string'ini al,
        // yoksa default local değeri kullan
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5433;Database=wholesaledb;Username=wholesale;Password=changeme";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connStr)
            .Options;

        return new AppDbContext(options);
    }
}
