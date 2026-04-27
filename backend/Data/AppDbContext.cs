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
    public DbSet<ProductUnitConfig> ProductUnitConfigs => Set<ProductUnitConfig>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StoreWholesaler> StoreWholesalers => Set<StoreWholesaler>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

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

        // Optimistic concurrency — PostgreSQL xmin system column (kolon oluşturmaz)
        mb.Entity<Product>().UseXminAsConcurrencyToken();

        // ProductUnitConfig → Product
        mb.Entity<ProductUnitConfig>().Property(u => u.Price).HasPrecision(18, 2);
        mb.Entity<ProductUnitConfig>()
            .HasOne(u => u.Product)
            .WithMany(p => p.UnitConfigs)
            .HasForeignKey(u => u.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProductBarcode → ProductUnitConfig
        mb.Entity<ProductBarcode>()
            .HasOne(b => b.UnitConfig)
            .WithMany(u => u.Barcodes)
            .HasForeignKey(b => b.UnitConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order
        mb.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
        mb.Entity<Order>().Property(o => o.Status).HasConversion<string>();

        // OrderItem
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

        // StockMovement — append-only ledger
        mb.Entity<StockMovement>()
            .Property(sm => sm.MovementType)
            .HasConversion<string>();

        mb.Entity<StockMovement>()
            .HasOne(sm => sm.Product)
            .WithMany()
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict); // ürün silindi diye ledger silinmez

        // Index: ProductId + CreatedAt (hareket geçmişi sorgusu)
        mb.Entity<StockMovement>()
            .HasIndex(sm => new { sm.ProductId, sm.CreatedAt })
            .HasDatabaseName("IX_StockMovements_ProductId_CreatedAt");

        // Index: OrderId (siparişe ait hareketler)
        mb.Entity<StockMovement>()
            .HasIndex(sm => sm.OrderId)
            .HasDatabaseName("IX_StockMovements_OrderId");
    }
}
