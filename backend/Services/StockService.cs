using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class StockService(
    AppDbContext db,
    IServiceScopeFactory scopeFactory,
    ILogger<StockService> logger) : IStockService
{
    public async Task ApplyMovementAsync(
        Guid productId,
        int qtyChange,
        MovementType type,
        Guid? orderId,
        Guid userId,
        string? reason = null)
    {
        // EF identity map: ürün zaten tracked ise DB'ye gitmiyor
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new KeyNotFoundException($"Ürün bulunamadı: {productId}");

        product.Stock += qtyChange;

        db.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            MovementType = type,
            QuantityChange = qtyChange,
            BalanceAfter = product.Stock,
            OrderId = orderId,
            UserId = userId,
            Reason = reason,
        });

        // MinimumStockLevel alarm — sadece stok azaldığında kontrol et
        if (qtyChange < 0 &&
            product.MinimumStockLevel.HasValue &&
            product.Stock <= product.MinimumStockLevel.Value)
        {
            logger.LogWarning(
                "LowStock: Product={ProductId} Name={Name} Stock={Stock} MinLevel={MinLevel}",
                productId, product.Name, product.Stock, product.MinimumStockLevel.Value);

            // Yeni scope ile fire-and-forget — request DbContext'ine dokunmaz
            var capturedId   = productId;
            var capturedName = product.Name;
            var capturedWholesalerId = product.WholesalerId;
            var capturedMinLevel = product.MinimumStockLevel.Value;
            var capturedBalance  = product.Stock;

            _ = Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var notif = scope.ServiceProvider.GetRequiredService<NotificationService>();
                try
                {
                    // Snapshot nesneyle bildirim — DB'ye ek sorgu atmıyor
                    var snapshot = new Product
                    {
                        Id = capturedId,
                        Name = capturedName,
                        WholesalerId = capturedWholesalerId,
                        MinimumStockLevel = capturedMinLevel,
                    };
                    await notif.LowStockAsync(snapshot, capturedBalance);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "LowStock bildirimi gönderilemedi: Product={ProductId}", capturedId);
                }
            });
        }
    }
}
