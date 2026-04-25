using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/credit")]
[Authorize]
public class CreditController(AppDbContext db) : ControllerBase
{
    // Toptancı — belirli bir mağazanın veresiye özeti
    [HttpGet("store/{storeId:guid}")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<CreditSummaryDto> GetStoreCredit(Guid storeId)
    {
        var wholesalerId = await GetWholesalerId();
        return await BuildSummary(storeId, wholesalerId);
    }

    // Toptancı — tüm mağazaların bakiyeleri (özet liste)
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
        foreach (var sw in stores)
        {
            var txs = await db.CreditTransactions
                .Where(c => c.StoreId == sw.StoreId && c.WholesalerId == wholesalerId)
                .ToListAsync();

            var debt = txs.Where(t => t.Type != CreditTransactionType.Payment).Sum(t => t.Amount);
            var paid = txs.Where(t => t.Type == CreditTransactionType.Payment).Sum(t => t.Amount);
            var overdue = txs
                .Where(t => t.Type != CreditTransactionType.Payment && t.DueDate < DateTime.UtcNow)
                .Sum(t => t.Amount);

            summaries.Add(new
            {
                storeId = sw.StoreId,
                storeName = sw.Store.StoreName,
                balance = debt - paid,
                overdueAmount = overdue
            });
        }

        return Ok(summaries);
    }

    // Mağaza — belirli bir toptancıdaki veresiye özeti
    [HttpGet("wholesaler/{wholesalerId:guid}")]
    [Authorize(Roles = "Store")]
    public async Task<CreditSummaryDto> GetWholesalerCredit(Guid wholesalerId)
    {
        var storeId = await GetStoreId();
        return await BuildSummary(storeId, wholesalerId);
    }

    // Toptancı — manuel işlem ekle (borç/ödeme)
    [HttpPost]
    [Authorize(Roles = "Wholesaler")]
    public async Task<IActionResult> AddTransaction([FromBody] CreateCreditTransactionDto dto)
    {
        var wholesalerId = await GetWholesalerId();

        if (!Enum.TryParse<CreditTransactionType>(dto.Type, out var type))
            throw new InvalidOperationException("Geçersiz işlem tipi");

        var tx = new CreditTransaction
        {
            StoreId = dto.StoreId,
            WholesalerId = wholesalerId,
            Type = type,
            Amount = dto.Amount,
            Description = dto.Description,
            DueDate = dto.DueDate
        };

        db.CreditTransactions.Add(tx);
        await db.SaveChangesAsync();
        return Ok(ToDto(tx));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<CreditSummaryDto> BuildSummary(Guid storeId, Guid wholesalerId)
    {
        var store = await db.Stores.FindAsync(storeId)
            ?? throw new KeyNotFoundException("Mağaza bulunamadı");

        var txs = await db.CreditTransactions
            .Where(c => c.StoreId == storeId && c.WholesalerId == wholesalerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var debt = txs.Where(t => t.Type != CreditTransactionType.Payment).Sum(t => t.Amount);
        var paid = txs.Where(t => t.Type == CreditTransactionType.Payment).Sum(t => t.Amount);
        var overdue = txs
            .Where(t => t.Type != CreditTransactionType.Payment && t.DueDate.HasValue && t.DueDate < DateTime.UtcNow)
            .Sum(t => t.Amount);

        return new CreditSummaryDto
        {
            StoreId = storeId,
            StoreName = store.StoreName,
            TotalDebt = debt,
            TotalPaid = paid,
            Balance = debt - paid,
            OverdueAmount = overdue,
            Transactions = txs.Select(ToDto).ToList()
        };
    }

    private static CreditTransactionDto ToDto(CreditTransaction t) => new()
    {
        Id = t.Id,
        Type = t.Type.ToString(),
        Amount = t.Amount,
        Description = t.Description,
        DueDate = t.DueDate,
        OrderId = t.OrderId,
        CreatedAt = t.CreatedAt
    };

    private async Task<Guid> GetWholesalerId()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var w = await db.Wholesalers.FirstOrDefaultAsync(w => w.UserId == userId)
            ?? throw new KeyNotFoundException("Toptancı bulunamadı");
        return w.Id;
    }

    private async Task<Guid> GetStoreId()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var s = await db.Stores.FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException("Mağaza bulunamadı");
        return s.Id;
    }
}
