using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class DeliveryNoteService(
    AppDbContext db,
    IAuditService auditService,
    IHttpContextAccessor http) : IDeliveryNoteService
{
    public async Task<DeliveryNoteDto> CreateFromOrderAsync(Guid orderId, Guid wholesalerId, CreateDeliveryNoteDto dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var order = await db.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Store)
            .Include(o => o.Wholesaler)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı");

        if (order.Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Sadece Confirmed durumundaki siparişler için sevk irsaliyesi oluşturulabilir");

        // Sequence-based unique note number
        var seq = await db.Database
            .SqlQueryRaw<long>("SELECT nextval('delivery_note_seq')")
            .ToListAsync();
        var noteNumber = $"SI-{dto.IssueDate:yyyyMMdd}-{seq[0]:D4}";

        var note = new DeliveryNote
        {
            NoteNumber = noteNumber,
            OrderId = orderId,
            WholesalerId = wholesalerId,
            StoreId = order.StoreId,
            Status = DeliveryNoteStatus.Issued,
            IssueDate = dto.IssueDate.Date == default ? DateTime.UtcNow.Date : dto.IssueDate,
            VehiclePlate = dto.VehiclePlate?.Trim().ToUpperInvariant(),
            DriverName = dto.DriverName?.Trim(),
            SourceAddress = dto.SourceAddress?.Trim() ?? order.Wholesaler.CompanyAddress ?? order.Wholesaler.Address,
            DestinationAddress = dto.DestinationAddress?.Trim() ?? order.Store.Address,
            Notes = dto.Notes?.Trim(),
        };

        db.DeliveryNotes.Add(note);
        await db.SaveChangesAsync();

        foreach (var item in order.Items)
        {
            var shipped = dto.ShippedQuantities != null && dto.ShippedQuantities.TryGetValue(item.ProductId, out var q)
                ? Math.Min(q, item.Quantity)
                : item.Quantity;

            db.DeliveryNoteItems.Add(new DeliveryNoteItem
            {
                DeliveryNoteId = note.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                ProductBrand = item.Product.Brand,
                UnitType = item.UnitType,
                ContentQty = item.ContentQty,
                UnitPrice = item.UnitPrice,
                VatRate = item.VatRate,
                QuantityOrdered = item.Quantity,
                QuantityShipped = shipped,
            });
        }

        // Siparişi Delivered'a geçir (aynı transaction)
        order.Status = OrderStatus.Delivered;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await tx.CommitAsync();

        auditService.LogAction(
            CurrentUserId, CurrentRole, "DeliveryNoteCreated", "DeliveryNote", note.Id.ToString(),
            new { NoteNumber = noteNumber, OrderId = orderId }, CurrentIp);

        return await GetByIdAsync(note.Id, wholesalerId);
    }

    public async Task<DeliveryNoteDto> CancelAsync(Guid noteId, Guid wholesalerId, CancelDeliveryNoteDto dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var note = await db.DeliveryNotes
            .Include(dn => dn.Order)
            .FirstOrDefaultAsync(dn => dn.Id == noteId && dn.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Sevk irsaliyesi bulunamadı");

        if (note.Status == DeliveryNoteStatus.Cancelled)
            throw new InvalidOperationException("İrsaliye zaten iptal edilmiş");

        // Sipariş durumunu geri al: Delivered → Confirmed
        var order = note.Order;
        if (!order.Status.CanTransitionTo(OrderStatus.Confirmed))
            throw new InvalidOperationException("Siparişin durumu Confirmed'a geri alınamıyor");

        note.Status = DeliveryNoteStatus.Cancelled;
        note.CancelledAt = DateTime.UtcNow;
        note.CancelReason = dto.CancelReason?.Trim();

        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        auditService.LogAction(
            CurrentUserId, CurrentRole, "DeliveryNoteCancelled", "DeliveryNote", noteId.ToString(),
            new { Reason = dto.CancelReason }, CurrentIp);

        return await GetByIdAsync(noteId, wholesalerId);
    }

    public async Task<DeliveryNoteDto> GetByIdAsync(Guid noteId, Guid wholesalerId)
    {
        var note = await db.DeliveryNotes
            .Include(dn => dn.Items)
            .Include(dn => dn.Wholesaler)
            .Include(dn => dn.Store)
            .FirstOrDefaultAsync(dn => dn.Id == noteId && dn.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Sevk irsaliyesi bulunamadı");

        return MapDto(note);
    }

    public async Task<List<DeliveryNoteListItemDto>> GetListAsync(
        Guid wholesalerId, Guid? storeId, DateTime? from, DateTime? to, string? status)
    {
        var q = db.DeliveryNotes
            .Include(dn => dn.Items)
            .Include(dn => dn.Store)
            .Include(dn => dn.Wholesaler)
            .Where(dn => dn.WholesalerId == wholesalerId)
            .AsQueryable();

        if (storeId.HasValue) q = q.Where(dn => dn.StoreId == storeId.Value);
        if (from.HasValue) q = q.Where(dn => dn.IssueDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(dn => dn.IssueDate <= to.Value.Date);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DeliveryNoteStatus>(status, true, out var st))
            q = q.Where(dn => dn.Status == st);

        var list = await q.OrderByDescending(dn => dn.CreatedAt).ToListAsync();
        return list.Select(MapListDto).ToList();
    }

    public async Task<List<DeliveryNoteListItemDto>> GetForStoreAsync(
        Guid storeId, DateTime? from, DateTime? to, string? status)
    {
        var q = db.DeliveryNotes
            .Include(dn => dn.Items)
            .Include(dn => dn.Store)
            .Include(dn => dn.Wholesaler)
            .Where(dn => dn.StoreId == storeId)
            .AsQueryable();

        if (from.HasValue) q = q.Where(dn => dn.IssueDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(dn => dn.IssueDate <= to.Value.Date);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DeliveryNoteStatus>(status, true, out var st))
            q = q.Where(dn => dn.Status == st);

        var list = await q.OrderByDescending(dn => dn.CreatedAt).ToListAsync();
        return list.Select(MapListDto).ToList();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static DeliveryNoteDto MapDto(DeliveryNote dn) => new()
    {
        Id = dn.Id,
        NoteNumber = dn.NoteNumber,
        OrderId = dn.OrderId,
        WholesalerId = dn.WholesalerId,
        WholesalerName = dn.Wholesaler.CompanyName,
        StoreId = dn.StoreId,
        StoreName = dn.Store.StoreName,
        Status = dn.Status.ToString(),
        IssueDate = dn.IssueDate,
        VehiclePlate = dn.VehiclePlate,
        DriverName = dn.DriverName,
        SourceAddress = dn.SourceAddress,
        DestinationAddress = dn.DestinationAddress,
        Notes = dn.Notes,
        Ettn = dn.Ettn,
        EInvoiceProvider = dn.EInvoiceProvider,
        EInvoiceStatus = dn.EInvoiceStatus,
        CreatedAt = dn.CreatedAt,
        CancelledAt = dn.CancelledAt,
        CancelReason = dn.CancelReason,
        Items = dn.Items.Select(i => new DeliveryNoteItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ProductBrand = i.ProductBrand,
            UnitType = i.UnitType,
            ContentQty = i.ContentQty,
            UnitPrice = i.UnitPrice,
            VatRate = i.VatRate,
            QuantityOrdered = i.QuantityOrdered,
            QuantityShipped = i.QuantityShipped,
            LineTotal = i.UnitPrice * i.QuantityShipped,
        }).ToList()
    };

    private static DeliveryNoteListItemDto MapListDto(DeliveryNote dn) => new()
    {
        Id = dn.Id,
        NoteNumber = dn.NoteNumber,
        OrderId = dn.OrderId,
        StoreName = dn.Store.StoreName,
        WholesalerName = dn.Wholesaler.CompanyName,
        Status = dn.Status.ToString(),
        IssueDate = dn.IssueDate,
        CreatedAt = dn.CreatedAt,
        ItemCount = dn.Items.Count,
        TotalAmount = dn.Items.Sum(i => i.UnitPrice * i.QuantityShipped),
    };

    private System.Security.Claims.ClaimsPrincipal? User => http.HttpContext?.User;

    private Guid? CurrentUserId =>
        Guid.TryParse(User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    private string CurrentRole =>
        User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "System";

    private string? CurrentIp =>
        http.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
