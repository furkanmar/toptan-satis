using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public interface IStockService
{
    /// <summary>
    /// Stok hareketi uygular: Product.Stock günceller + StockMovement ekler.
    /// SaveChanges ÇAĞIRMAZ — caller transaction + save'den sorumlu.
    /// </summary>
    Task ApplyMovementAsync(
        Guid productId,
        int qtyChange,
        MovementType type,
        Guid? orderId,
        Guid userId,
        string? reason = null);
}
