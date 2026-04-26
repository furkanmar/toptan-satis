using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class OrderService(AppDbContext db)
{
    public async Task<OrderDto> CreateAsync(Guid storeId, CreateOrderDto dto)
    {
        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products
            .Include(p => p.UnitConfigs)
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        if (products.Count != dto.Items.Count)
            throw new InvalidOperationException("Bazı ürünler bulunamadı veya aktif değil");

        var distinctWholesalers = products.Select(p => p.WholesalerId).Distinct().ToList();
        if (distinctWholesalers.Count > 1)
            throw new InvalidOperationException("Sipariş tek toptancıdan olmalı");

        var items = new List<OrderItem>();
        foreach (var i in dto.Items)
        {
            var p = products.First(p => p.Id == i.ProductId);
            ProductUnitConfig? unitConfig = null;

            if (i.UnitConfigId.HasValue)
                unitConfig = p.UnitConfigs.FirstOrDefault(u => u.Id == i.UnitConfigId.Value);

            // Fallback: ilk unit config ya da base price
            unitConfig ??= p.UnitConfigs.OrderBy(u => u.SortOrder).FirstOrDefault();

            items.Add(new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = unitConfig?.Price ?? p.Price,
                UnitType = unitConfig?.UnitType ?? "Adet",
                ContentQty = unitConfig?.ContentQty ?? 1,
                VatRate = p.VatRate,
            });
        }

        var order = new Order
        {
            StoreId = storeId,
            WholesalerId = distinctWholesalers[0],
            Note = dto.Note,
            Items = items,
            TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice)
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return await GetByIdAsync(order.Id);
    }

    public async Task<OrderDto> ConfirmAsync(Guid orderId, Guid wholesalerId, ConfirmOrderDto dto)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Sadece bekleyen siparişler onaylanabilir");

        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        foreach (var item in order.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            var totalUnits = item.Quantity * item.ContentQty;
            if (product.Stock < totalUnits && !dto.ForceConfirm)
                throw new InvalidOperationException($"'{product.Name}' için yeterli stok yok (mevcut: {product.Stock})");
            product.Stock -= totalUnits;
        }

        order.Status = OrderStatus.Confirmed;
        order.DueDate = dto.DueDate;
        order.WholesalerNote = dto.WholesalerNote;
        order.UpdatedAt = DateTime.UtcNow;

        if (dto.CreateCreditEntry)
        {
            db.CreditTransactions.Add(new CreditTransaction
            {
                StoreId = order.StoreId,
                WholesalerId = wholesalerId,
                Type = CreditTransactionType.OrderDebit,
                Amount = order.TotalAmount,
                Description = $"Sipariş #{order.Id.ToString()[..8].ToUpper()}",
                DueDate = dto.DueDate,
                OrderId = order.Id
            });
        }

        await db.SaveChangesAsync();
        return await GetByIdAsync(orderId);
    }

    public async Task<OrderDto> UpdateItemsAsync(Guid orderId, Guid wholesalerId, UpdateOrderItemsDto dto)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Sadece bekleyen siparişlerin içeriği değiştirilebilir");

        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products
            .Include(p => p.UnitConfigs)
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        if (products.Count != dto.Items.Count)
            throw new InvalidOperationException("Bazı ürünler bulunamadı");

        db.OrderItems.RemoveRange(order.Items);

        var newItems = new List<OrderItem>();
        foreach (var i in dto.Items)
        {
            var p = products.First(p => p.Id == i.ProductId);
            var unitConfig = i.UnitConfigId.HasValue
                ? p.UnitConfigs.FirstOrDefault(u => u.Id == i.UnitConfigId.Value)
                : p.UnitConfigs.OrderBy(u => u.SortOrder).FirstOrDefault();

            newItems.Add(new OrderItem
            {
                OrderId = orderId,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = unitConfig?.Price ?? p.Price,
                UnitType = unitConfig?.UnitType ?? "Adet",
                ContentQty = unitConfig?.ContentQty ?? 1,
                VatRate = p.VatRate,
            });
        }

        order.Items = newItems;
        order.TotalAmount = newItems.Sum(i => i.Quantity * i.UnitPrice);
        if (dto.WholesalerNote != null) order.WholesalerNote = dto.WholesalerNote;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return await GetByIdAsync(orderId);
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid orderId, Guid wholesalerId, OrderStatus newStatus)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı");

        if (newStatus == OrderStatus.Confirmed && order.Status == OrderStatus.Pending)
        {
            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                var totalUnits = item.Quantity * item.ContentQty;
                if (product.Stock < totalUnits)
                    throw new InvalidOperationException($"'{product.Name}' için yeterli stok yok (mevcut: {product.Stock})");
                product.Stock -= totalUnits;
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
        WholesalerNote = o.WholesalerNote,
        DueDate = o.DueDate,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Total = i.Quantity * i.UnitPrice,
            UnitType = i.UnitType,
            ContentQty = i.ContentQty,
            VatRate = i.VatRate,
        }).ToList()
    };
}
