using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;
using WholesaleApi.Exceptions;
using WholesaleApi.Services;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/credit")]
[Authorize]
public class CreditController(AppDbContext db, ICreditService creditService) : ControllerBase
{
    // ─── Özet Endpointleri ───────────────────────────────────────────────────

    /// <summary>Toptancı — belirli bir mağazanın veresiye özeti.</summary>
    [HttpGet("store/{storeId:guid}")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<CreditSummaryDto> GetStoreCredit(Guid storeId)
    {
        var wholesalerId = await GetWholesalerId();
        return await creditService.GetSummaryAsync(storeId, wholesalerId);
    }

    /// <summary>Mağaza — belirli bir toptancıdaki veresiye özeti.</summary>
    [HttpGet("wholesaler/{wholesalerId:guid}")]
    [Authorize(Roles = "Store")]
    public async Task<CreditSummaryDto> GetWholesalerCredit(Guid wholesalerId)
    {
        var storeId = await GetStoreId();
        return await creditService.GetSummaryAsync(storeId, wholesalerId);
    }

    /// <summary>Toptancı — tüm aktif mağazaların bakiye özet listesi.</summary>
    [HttpGet("all-stores")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> GetAllStores()
    {
        var wholesalerId = await GetWholesalerId();

        var stores = await db.StoreWholesalers
            .Include(sw => sw.Store)
            .Where(sw => sw.WholesalerId == wholesalerId && sw.IsActive)
            .ToListAsync();

        var summaries = new List<object>();
        var now = DateTime.UtcNow;

        foreach (var sw in stores)
        {
            var txs = await db.CreditTransactions
                .Where(c => c.StoreId == sw.StoreId && c.WholesalerId == wholesalerId)
                .ToListAsync();

            var debt    = txs.Where(t => t.Type != CreditTransactionType.Payment).Sum(t => t.Amount);
            var paid    = txs.Where(t => t.Type == CreditTransactionType.Payment).Sum(t => t.Amount);
            var overdue = txs
                .Where(t =>
                    t.Type != CreditTransactionType.Payment &&
                    t.DueDate.HasValue &&
                    t.DueDate < now &&
                    !t.IsFullyAllocated)
                .Sum(t => t.Amount - t.AllocatedAmount);

            summaries.Add(new
            {
                storeId     = sw.StoreId,
                storeName   = sw.Store.StoreName,
                balance     = debt - paid,
                overdueAmount = overdue,
            });
        }

        return Ok(summaries);
    }

    // ─── Ekstre ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Tarih filtreli ekstre — opening/running/closing balance dahil.
    /// Wholesaler kendi mağazaları için, Store kendi hesabı için, Admin tümü.
    /// </summary>
    [HttpGet("statement")]
    public async Task<IActionResult> GetStatement(
        [FromQuery] Guid storeId,
        [FromQuery] Guid wholesalerId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);

        switch (role)
        {
            case "Wholesaler":
            {
                var wId = await GetWholesalerId();
                if (wId != wholesalerId)
                    throw new ForbiddenException("Bu mağazanın ekstresine erişim yetkisi yok");
                break;
            }
            case "Store":
            {
                var sId = await GetStoreId();
                if (sId != storeId)
                    throw new ForbiddenException("Başka mağazanın ekstresine erişim yetkisi yok");
                break;
            }
            // Admin: kısıtlama yok
        }

        var result = await creditService.GetStatementAsync(storeId, wholesalerId, from, to);
        return Ok(result);
    }

    // ─── İşlem Oluşturma ─────────────────────────────────────────────────────

    /// <summary>Toptancı — manuel borç veya ödeme ekle.</summary>
    [HttpPost]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> AddTransaction([FromBody] CreateCreditTransactionDto dto)
    {
        var wholesalerId = await GetWholesalerId();

        if (!Enum.TryParse<CreditTransactionType>(dto.Type, out var type))
            throw new InvalidOperationException("Geçersiz işlem tipi. Kabul edilenler: ManualDebit, Payment");

        switch (type)
        {
            case CreditTransactionType.Payment:
                var result = await creditService.RecordPaymentAsync(
                    dto.StoreId, wholesalerId, dto.Amount, dto.Description);
                return Ok(result);

            case CreditTransactionType.ManualDebit:
                await creditService.ManualDebitAsync(
                    dto.StoreId, wholesalerId, dto.Amount, dto.Description, dto.DueDate);
                return Ok();

            default:
                throw new InvalidOperationException("OrderDebit bu endpoint üzerinden eklenemez");
        }
    }

    // ─── Admin: Manuel Allocation ─────────────────────────────────────────────

    /// <summary>Admin — FIFO dışı manuel allocation oluştur (hatalı dağıtım düzeltme).</summary>
    [HttpPost("/api/admin/credit/allocations")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAllocation([FromBody] CreateManualAllocationDto dto)
    {
        var adminUserId = GetUserId();
        var allocation = await creditService.CreateManualAllocationAsync(
            dto.PaymentTransactionId,
            dto.DebitTransactionId,
            dto.Amount,
            adminUserId);
        return Ok(allocation);
    }

    /// <summary>Admin — allocation iptal et (Debit ve Payment AllocatedAmount rollback).</summary>
    [HttpDelete("/api/admin/credit/allocations/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAllocation(Guid id)
    {
        var adminUserId = GetUserId();
        await creditService.DeleteAllocationAsync(id, adminUserId);
        return NoContent();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid> GetWholesalerId()
    {
        var userId = GetUserId();
        var w = await db.Wholesalers.FirstOrDefaultAsync(w => w.UserId == userId)
            ?? throw new KeyNotFoundException("Toptancı bulunamadı");
        return w.Id;
    }

    private async Task<Guid> GetStoreId()
    {
        var userId = GetUserId();
        var s = await db.Stores.FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException("Mağaza bulunamadı");
        return s.Id;
    }
}
