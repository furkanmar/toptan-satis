using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class StockService(AppDbContext db) : IStockService
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
    }
}
