using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;
using WholesaleApi.Exceptions;

namespace WholesaleApi.Services;

public class OrderService(
    AppDbContext db,
    IStockService stockService,
    IAuditService auditService,
    NotificationService notifications,
    IHttpContextAccessor httpContextAccessor,
    ILogger<OrderService> logger)
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

        // StoreWholesaler ilişki kontrolü
        var targetWholesalerId = distinctWholesalers[0];
        var relation = await db.StoreWholesalers
            .FirstOrDefaultAsync(sw => sw.StoreId == storeId && sw.WholesalerId == targetWholesalerId);

        if (relation is null)
            throw new ForbiddenException("Bu toptancıya sipariş verme yetkiniz yok");
        if (!relation.IsActive)
            throw new ForbiddenException("Bu toptancıyla ilişkiniz askıya alınmış");

        var items = new List<OrderItem>();
        foreach (var i in dto.Items)
        {
            var p = products.First(p => p.Id == i.ProductId);
            ProductUnitConfig? unitConfig = null;

            if (i.UnitConfigId.HasValue)
                unitConfig = p.UnitConfigs.FirstOrDefault(u => u.Id == i.UnitConfigId.Value);

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
            WholesalerId = targetWholesalerId,
            Note = dto.Note,
            Items = items,
            TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice)
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Toptancıya yeni sipariş bildirimi (fire-and-forget)
        var created = await BuildQuery().FirstOrDefaultAsync(o => o.Id == order.Id);
        if (created is not null)
            _ = notifications.OrderCreatedAsync(created);

        return await GetByIdAsync(order.Id);
    }

    /// <summary>
    /// Siparişi onayla. xmin concurrency token ile optimistic lock;
    /// çakışmada max 3 deneme yapar, başarısız olursa 409 fırlatır.
    /// </summary>
    public async Task<OrderDto> ConfirmAsync(
        Guid orderId,
        Guid wholesalerId,
        Guid userId,
        ConfirmOrderDto dto)
    {
        DbUpdateConcurrencyException? lastConcurrencyEx = null;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return await ExecuteConfirmAsync(orderId, wholesalerId, userId, dto);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                lastConcurrencyEx = ex;
                db.ChangeTracker.Clear();
                logger.LogWarning(
                    "Concurrency çakışması (deneme {Attempt}/3): Order={OrderId}",
                    attempt + 1, orderId);
            }
        }

        throw lastConcurrencyEx!; // Middleware → 409
    }

    private async Task<OrderDto> ExecuteConfirmAsync(
        Guid orderId,
        Guid wholesalerId,
        Guid userId,
        ConfirmOrderDto dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı");

        if (!order.Status.CanTransitionTo(OrderStatus.Confirmed))
            throw new InvalidOperationException(
                $"Sipariş '{order.Status}' durumundayken onaylanamaz");

        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        foreach (var item in order.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            var totalUnits = item.Quantity * item.ContentQty;
            bool goingNegative = product.Stock - totalUnits < 0;

            if (goingNegative && !dto.ForceConfirm)
                throw new InvalidOperationException(
                    $"'{product.Name}' için yeterli stok yok (mevcut: {product.Stock})");

            var movType = goingNegative
                ? MovementType.ForceConfirmNegative
                : MovementType.OrderConfirm;

            if (goingNegative)
                logger.LogWarning(
                    "ForceConfirm negatif stok: Order={OrderId} Product={ProductId} " +
                    "ProductName={ProductName} CurrentStock={Stock} Deduct={Deduct} WillBe={WillBe}",
                    orderId, item.ProductId, product.Name,
                    product.Stock, totalUnits, product.Stock - totalUnits);

            await stockService.ApplyMovementAsync(
                item.ProductId, -totalUnits, movType, orderId, userId);
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

        auditService.LogAction(
            userId,
            CurrentRole,
            "OrderConfirmed",
            "Order",
            orderId.ToString(),
            new { dto.ForceConfirm, dto.DueDate, dto.CreateCreditEntry },
            CurrentIp);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        // Mağazaya onay bildirimi (fire-and-forget)
        var confirmed = await BuildQuery().FirstOrDefaultAsync(o => o.Id == orderId);
        if (confirmed is not null)
            _ = notifications.OrderConfirmedAsync(confirmed);

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

    /// <summary>
    /// Sipariş durumunu güncelle. State machine ihlalinde 400 fırlatır.
    /// Confirmed → Cancelled: stok geri yükler ve ledger kaydı oluşturur.
    /// </summary>
    public async Task<OrderDto> UpdateStatusAsync(
        Guid orderId,
        Guid wholesalerId,
        Guid userId,
        OrderStatus newStatus)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı");

        var oldStatus = order.Status;

        if (!order.Status.CanTransitionTo(newStatus))
            throw new InvalidOperationException(
                $"'{order.Status}' → '{newStatus}' geçişi geçersiz");

        // Onaylanmış → İptal: stok iade
        if (oldStatus == OrderStatus.Confirmed && newStatus == OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                var totalUnits = item.Quantity * item.ContentQty;
                await stockService.ApplyMovementAsync(
                    item.ProductId, +totalUnits, MovementType.OrderCancel, orderId, userId);
            }
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        var actionName = newStatus switch
        {
            OrderStatus.Cancelled => "OrderCancelled",
            OrderStatus.Rejected  => "OrderRejected",
            OrderStatus.Delivered => "OrderDelivered",
            _                     => $"OrderStatus{newStatus}"
        };

        auditService.LogAction(
            userId,
            CurrentRole,
            actionName,
            "Order",
            orderId.ToString(),
            new { From = oldStatus.ToString(), To = newStatus.ToString() },
            CurrentIp);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        // Bildirim (fire-and-forget)
        var updated = await BuildQuery().FirstOrDefaultAsync(o => o.Id == orderId);
        if (updated is not null)
        {
            _ = newStatus switch
            {
                OrderStatus.Rejected  => notifications.OrderRejectedAsync(updated),
                OrderStatus.Cancelled => notifications.OrderCancelledAsync(
                    updated, cancelledByWholesaler: CurrentRole == "Wholesaler"),
                _                     => Task.CompletedTask
            };
        }

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

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private string CurrentRole =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? "System";

    private string? CurrentIp =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

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
