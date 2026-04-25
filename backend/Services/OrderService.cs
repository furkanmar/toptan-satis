using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class OrderService(AppDbContext db)
{
    public async Task<OrderDto> CreateAsync(Guid storeId, CreateOrderDto dto)
    {
        // Tüm ürünler aynı toptancıdan gelmeli
        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        if (products.Count != dto.Items.Count)
            throw new InvalidOperationException("Bazı ürünler bulunamadı veya aktif değil");

        var wholesalerId = products.Select(p => p.WholesalerId).Distinct().ToList();
        if (wholesalerId.Count > 1)
            throw new InvalidOperationException("Sipariş tek toptancıdan olmalı");

        var order = new Order
        {
            StoreId = storeId,
            WholesalerId = wholesalerId[0],
            Note = dto.Note,
            Items = dto.Items.Select(i =>
            {
                var p = products.First(p => p.Id == i.ProductId);
                return new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = p.Price
                };
            }).ToList()
        };

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return await GetByIdAsync(order.Id);
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid orderId, Guid wholesalerId, OrderStatus newStatus)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı");

        // Sipariş onaylanınca stok düş
        if (newStatus == OrderStatus.Confirmed && order.Status == OrderStatus.Pending)
        {
            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                if (product.Stock < item.Quantity)
                    throw new InvalidOperationException($"'{product.Name}' için yeterli stok yok (mevcut: {product.Stock})");
                product.Stock -= item.Quantity;
            }
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await GetByIdAsync(orderId);
    }

    public async Task<List<OrderDto>> GetForStoreAsync(Guid storeId)
        => await BuildQuery().Where(o => o.StoreId == storeId).Select(o => ToDto(o)).ToListAsync();

    public async Task<List<OrderDto>> GetForWholesalerAsync(Guid wholesalerId)
        => await BuildQuery().Where(o => o.WholesalerId == wholesalerId).Select(o => ToDto(o)).ToListAsync();

    public async Task<List<OrderDto>> GetAllAsync()
        => await BuildQuery().Select(o => ToDto(o)).ToListAsync();

    public async Task<OrderDto> GetByIdAsync(Guid id)
    {
        var order = await BuildQuery().FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı");
        return ToDto(order);
    }

    private IQueryable<Order> BuildQuery() =>
        db.Orders
            .Include(o => o.Store)
            .Include(o => o.Wholesaler)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt);

    private static OrderDto ToDto(Order o) => new()
    {
        Id = o.Id,
        StoreId = o.StoreId,
        StoreName = o.Store.StoreName,
        WholesalerId = o.WholesalerId,
        WholesalerName = o.Wholesaler.CompanyName,
        Status = o.Status.ToString(),
        TotalAmount = o.TotalAmount,
        Note = o.Note,
        CreatedAt = o.CreatedAt,
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Total = i.Quantity * i.UnitPrice
        }).ToList()
    };
}
