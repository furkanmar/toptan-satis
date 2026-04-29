using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Services;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/delivery-notes")]
[Authorize]
public class DeliveryNotesController(
    AppDbContext db,
    IDeliveryNoteService deliveryNoteService,
    DeliveryNotePdfService pdfService) : ControllerBase
{
    // ─── Toptancı Endpointleri ────────────────────────────────────────────────

    /// <summary>Onaylı sipariş için sevk irsaliyesi oluştur (Confirmed → Delivered).</summary>
    [HttpPost("orders/{orderId:guid}")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<DeliveryNoteDto> Create(Guid orderId, CreateDeliveryNoteDto dto)
    {
        var wholesalerId = await GetWholesalerId();
        return await deliveryNoteService.CreateFromOrderAsync(orderId, wholesalerId, dto);
    }

    /// <summary>Toptancının kendi irsaliyelerini listele.</summary>
    [HttpGet]
    [Authorize(Roles = "Wholesaler")]
    public async Task<List<DeliveryNoteListItemDto>> GetList(
        [FromQuery] Guid? storeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status)
    {
        var wholesalerId = await GetWholesalerId();
        return await deliveryNoteService.GetListAsync(wholesalerId, storeId, from, to, status);
    }

    /// <summary>Toptancı — tek irsaliye detayı.</summary>
    [HttpGet("{noteId:guid}")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<DeliveryNoteDto> GetById(Guid noteId)
    {
        var wholesalerId = await GetWholesalerId();
        return await deliveryNoteService.GetByIdAsync(noteId, wholesalerId);
    }

    /// <summary>Toptancı — irsaliye PDF indir.</summary>
    [HttpGet("{noteId:guid}/pdf")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> GetPdf(Guid noteId)
    {
        var wholesalerId = await GetWholesalerId();
        var bytes = await pdfService.GenerateAsync(noteId, wholesalerId);
        return File(bytes, "application/pdf", $"irsaliye-{noteId}.pdf");
    }

    /// <summary>İrsaliyeyi iptal et (Delivered → Confirmed).</summary>
    [HttpPost("{noteId:guid}/cancel")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<DeliveryNoteDto> Cancel(Guid noteId, CancelDeliveryNoteDto dto)
    {
        var wholesalerId = await GetWholesalerId();
        return await deliveryNoteService.CancelAsync(noteId, wholesalerId, dto);
    }

    // ─── Mağaza Endpointleri ──────────────────────────────────────────────────

    /// <summary>Mağaza — kendi irsaliyelerini listele.</summary>
    [HttpGet("store")]
    [Authorize(Roles = "Store")]
    public async Task<List<DeliveryNoteListItemDto>> GetForStore(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status)
    {
        var storeId = await GetStoreId();
        return await deliveryNoteService.GetForStoreAsync(storeId, from, to, status);
    }

    /// <summary>Mağaza — irsaliye PDF indir (noteId ile).</summary>
    [HttpGet("{noteId:guid}/pdf/store")]
    [Authorize(Roles = "Store")]
    public async Task<IActionResult> GetPdfForStore(Guid noteId)
    {
        var storeId = await GetStoreId();
        var bytes = await pdfService.GenerateForStoreAsync(noteId, storeId);
        return File(bytes, "application/pdf", $"irsaliye-{noteId}.pdf");
    }

    /// <summary>Mağaza — sipariş bazlı irsaliye PDF indir.</summary>
    [HttpGet("by-order/{orderId:guid}/pdf/store")]
    [Authorize(Roles = "Store")]
    public async Task<IActionResult> GetPdfByOrderForStore(Guid orderId)
    {
        var storeId = await GetStoreId();
        var note = await db.DeliveryNotes
            .FirstOrDefaultAsync(dn => dn.OrderId == orderId && dn.StoreId == storeId && dn.Status != WholesaleApi.Entities.DeliveryNoteStatus.Cancelled)
            ?? throw new KeyNotFoundException("Bu sipariş için irsaliye bulunamadı");
        var bytes = await pdfService.GenerateForStoreAsync(note.Id, storeId);
        return File(bytes, "application/pdf", $"irsaliye-{note.NoteNumber}.pdf");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Guid> GetWholesalerId()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var wholesaler = await db.Wholesalers.FirstOrDefaultAsync(w => w.UserId == userId)
            ?? throw new UnauthorizedAccessException("Toptancı profili bulunamadı");
        return wholesaler.Id;
    }

    private async Task<Guid> GetStoreId()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var store = await db.Stores.FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new UnauthorizedAccessException("Mağaza profili bulunamadı");
        return store.Id;
    }
}
