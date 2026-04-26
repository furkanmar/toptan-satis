using Microsoft.EntityFrameworkCore;
using WholesaleApi.Entities;

namespace WholesaleApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Wholesaler> Wholesalers => Set<Wholesaler>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<CatalogItemBarcode> CatalogItemBarcodes => Set<CatalogItemBarcode>();
    public DbSet<CatalogItemImage> CatalogItemImages => Set<CatalogItemImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StoreWholesaler> StoreWholesalers => Set<StoreWholesaler>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // User
        mb.Entity<User>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<User>().Property(u => u.Role).HasConversion<string>();

        // Wholesaler → User (1:1)
        mb.Entity<Wholesaler>()
            .HasOne(w => w.User)
            .WithOne(u => u.Wholesaler)
            .HasForeignKey<Wholesaler>(w => w.UserId);

        // Store → User (1:1)
        mb.Entity<Store>()
            .HasOne(s => s.User)
            .WithOne(u => u.Store)
            .HasForeignKey<Store>(s => s.UserId);

        // Product
        mb.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        mb.Entity<Product>().Property(p => p.Unit).HasConversion<string>();
        mb.Entity<Product>()
            .HasOne(p => p.CatalogItem)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CatalogItemId)
            .IsRequired(false);

        // CatalogItem
        mb.Entity<CatalogItem>().Property(c => c.Unit).HasConversion<string>();
        mb.Entity<CatalogItemBarcode>()
            .HasOne(b => b.CatalogItem)
            .WithMany(c => c.Barcodes)
            .HasForeignKey(b => b.CatalogItemId);
        mb.Entity<CatalogItemImage>()
            .HasOne(i => i.CatalogItem)
            .WithMany(c => c.Images)
            .HasForeignKey(i => i.CatalogItemId);

        // Order
        mb.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
        mb.Entity<Order>().Property(o => o.Status).HasConversion<string>();

        // OrderItem — fiyat snapshot
        mb.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasPrecision(18, 2);

        // Category — per-wholesaler, FK optional
        mb.Entity<Category>()
            .HasOne(c => c.Wholesaler)
            .WithMany(w => w.Categories)
            .HasForeignKey(c => c.WholesalerId)
            .IsRequired(false);

        // StoreWholesaler — composite PK
        mb.Entity<StoreWholesaler>()
            .HasKey(sw => new { sw.StoreId, sw.WholesalerId });

        mb.Entity<StoreWholesaler>()
            .HasOne(sw => sw.Store)
            .WithMany(s => s.StoreWholesalers)
            .HasForeignKey(sw => sw.StoreId);

        mb.Entity<StoreWholesaler>()
            .HasOne(sw => sw.Wholesaler)
            .WithMany(w => w.StoreWholesalers)
            .HasForeignKey(sw => sw.WholesalerId);

        // CreditTransaction
        mb.Entity<CreditTransaction>().Property(c => c.Amount).HasPrecision(18, 2);
        mb.Entity<CreditTransaction>().Property(c => c.Type).HasConversion<string>();

        mb.Entity<CreditTransaction>()
            .HasOne(c => c.Order)
            .WithMany(o => o.CreditTransactions)
            .HasForeignKey(c => c.OrderId)
            .IsRequired(false);
    }
}
