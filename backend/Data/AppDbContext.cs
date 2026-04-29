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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

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

        // Optimistic concurrency — PostgreSQL xmin system column (kolon oluşturmaz).
        // UseXminAsConcurrencyToken() extension'ının yaptığı şey tam olarak budur.
        mb.Entity<Product>()
            .Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

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
        mb.Entity<CreditTransaction>().Property(c => c.AllocatedAmount).HasPrecision(18, 2);
        mb.Entity<CreditTransaction>().Property(c => c.Type).HasConversion<string>();

        // RemainingAmount hesaplanan property — DB'ye yazılmaz
        mb.Entity<CreditTransaction>().Ignore(c => c.RemainingAmount);

        // Optimistic concurrency — PostgreSQL xmin (fiziksel kolon eklemez)
        mb.Entity<CreditTransaction>()
            .Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        mb.Entity<CreditTransaction>()
            .HasIndex(c => new { c.StoreId, c.WholesalerId, c.IsFullyAllocated, c.CreatedAt })
            .HasDatabaseName("IX_CreditTransactions_StoreWholesaler_Allocation");

        mb.Entity<CreditTransaction>()
            .HasOne(c => c.Order)
            .WithMany(o => o.CreditTransactions)
            .HasForeignKey(c => c.OrderId)
            .IsRequired(false);

        // PaymentAllocation
        mb.Entity<PaymentAllocation>().Property(pa => pa.AllocatedAmount).HasPrecision(18, 2);

        mb.Entity<PaymentAllocation>()
            .HasOne(pa => pa.PaymentTransaction)
            .WithMany(ct => ct.PaymentAllocations)
            .HasForeignKey(pa => pa.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<PaymentAllocation>()
            .HasOne(pa => pa.DebitTransaction)
            .WithMany(ct => ct.DebitAllocations)
            .HasForeignKey(pa => pa.DebitTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<PaymentAllocation>()
            .HasIndex(pa => pa.PaymentTransactionId)
            .HasDatabaseName("IX_PaymentAllocations_PaymentTransactionId");

        mb.Entity<PaymentAllocation>()
            .HasIndex(pa => pa.DebitTransactionId)
            .HasDatabaseName("IX_PaymentAllocations_DebitTransactionId");

        // StockMovement — append-only ledger
        mb.Entity<StockMovement>()
            .Property(sm => sm.MovementType)
            .HasConversion<string>();

        mb.Entity<StockMovement>()
            .HasOne(sm => sm.Product)
            .WithMany()
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<StockMovement>()
            .HasIndex(sm => new { sm.ProductId, sm.CreatedAt })
            .HasDatabaseName("IX_StockMovements_ProductId_CreatedAt");

        mb.Entity<StockMovement>()
            .HasIndex(sm => sm.OrderId)
            .HasDatabaseName("IX_StockMovements_OrderId");

        // AuditLog — append-only denetim defteri
        mb.Entity<AuditLog>()
            .Property(a => a.Changes)
            .HasColumnType("jsonb");

        mb.Entity<AuditLog>()
            .HasIndex(a => new { a.UserId, a.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_UserId_CreatedAt");

        mb.Entity<AuditLog>()
            .HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");

        // IdempotencyKey — unique index on Key, partial TTL cleanup
        mb.Entity<IdempotencyKey>()
            .HasIndex(ik => ik.Key)
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyKeys_Key");

        mb.Entity<IdempotencyKey>()
            .HasIndex(ik => ik.ExpiresAt)
            .HasDatabaseName("IX_IdempotencyKeys_ExpiresAt");

        // NotificationLog — enum → string
        mb.Entity<NotificationLog>()
            .Property(nl => nl.Type).HasConversion<string>();

        mb.Entity<NotificationLog>()
            .Property(nl => nl.Channel).HasConversion<string>();

        mb.Entity<NotificationLog>()
            .Property(nl => nl.Status).HasConversion<string>();

        mb.Entity<NotificationLog>()
            .HasIndex(nl => new { nl.UserId, nl.CreatedAt })
            .HasDatabaseName("IX_NotificationLogs_UserId_CreatedAt");

        mb.Entity<NotificationLog>()
            .HasIndex(nl => nl.Status)
            .HasDatabaseName("IX_NotificationLogs_Status");
    }
}
