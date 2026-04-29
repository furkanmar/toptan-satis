using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class CreditService(
    AppDbContext db,
    IAuditService auditService,
    ILogger<CreditService> logger) : ICreditService
{
    // ─── Borç Kayıtları ───────────────────────────────────────────────────────

    public Task RecordOrderDebitAsync(
        Guid storeId,
        Guid wholesalerId,
        decimal amount,
        string description,
        DateTime? dueDate,
        Guid orderId)
    {
        db.CreditTransactions.Add(new CreditTransaction
        {
            StoreId = storeId,
            WholesalerId = wholesalerId,
            Type = CreditTransactionType.OrderDebit,
            Amount = amount,
            Description = description,
            DueDate = dueDate,
            OrderId = orderId,
        });
        // SaveChanges çağıranın sorumluluğunda (OrderService kendi tx'ini yönetiyor)
        return Task.CompletedTask;
    }

    public async Task ManualDebitAsync(
        Guid storeId,
        Guid wholesalerId,
        decimal amount,
        string description,
        DateTime? dueDate)
    {
        db.CreditTransactions.Add(new CreditTransaction
        {
            StoreId = storeId,
            WholesalerId = wholesalerId,
            Type = CreditTransactionType.ManualDebit,
            Amount = amount,
            Description = description,
            DueDate = dueDate,
        });
        await db.SaveChangesAsync();
    }

    // ─── Ödeme + FIFO ─────────────────────────────────────────────────────────

    public async Task<PaymentResult> RecordPaymentAsync(
        Guid storeId,
        Guid wholesalerId,
        decimal amount,
        string description)
    {
        DbUpdateConcurrencyException? lastEx = null;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return await ExecutePaymentAsync(storeId, wholesalerId, amount, description);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                lastEx = ex;
                db.ChangeTracker.Clear();
                logger.LogWarning(
                    "Concurrency çakışması (deneme {Attempt}/3): RecordPayment StoreId={StoreId}",
                    attempt + 1, storeId);
            }
        }

        throw lastEx!; // ExceptionMiddleware → 409
    }

    private async Task<PaymentResult> ExecutePaymentAsync(
        Guid storeId,
        Guid wholesalerId,
        decimal amount,
        string description)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        // Payment transaction'ı oluştur
        var payment = new CreditTransaction
        {
            StoreId = storeId,
            WholesalerId = wholesalerId,
            Type = CreditTransactionType.Payment,
            Amount = amount,
            Description = description,
            AllocatedAmount = 0,
            IsFullyAllocated = false,
        };
        db.CreditTransactions.Add(payment);
        await db.SaveChangesAsync(); // ID ve xmin alıyoruz

        // FIFO: en eski açık debit'leri sırayla çek
        var openDebits = await db.CreditTransactions
            .Where(c =>
                c.StoreId == storeId &&
                c.WholesalerId == wholesalerId &&
                (c.Type == CreditTransactionType.OrderDebit || c.Type == CreditTransactionType.ManualDebit) &&
                !c.IsFullyAllocated)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        decimal remainingPayment = amount;

        foreach (var debit in openDebits)
        {
            if (remainingPayment <= 0) break;

            decimal remainingDebit = debit.Amount - debit.AllocatedAmount;
            if (remainingDebit <= 0) continue;

            decimal allocated = Math.Min(remainingDebit, remainingPayment);

            db.PaymentAllocations.Add(new PaymentAllocation
            {
                PaymentTransactionId = payment.Id,
                DebitTransactionId = debit.Id,
                AllocatedAmount = allocated,
            });

            debit.AllocatedAmount += allocated;
            debit.IsFullyAllocated = debit.AllocatedAmount >= debit.Amount;

            payment.AllocatedAmount += allocated;
            remainingPayment -= allocated;
        }

        // Tüm ödeme borçlara dağıtıldıysa fully allocated
        payment.IsFullyAllocated = remainingPayment == 0;

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new PaymentResult
        {
            TransactionId = payment.Id,
            TotalAmount = amount,
            AllocatedAmount = payment.AllocatedAmount,
            UnallocatedAmount = remainingPayment,
        };
    }

    // ─── Özet ────────────────────────────────────────────────────────────────

    public async Task<CreditSummaryDto> GetSummaryAsync(Guid storeId, Guid wholesalerId)
    {
        var store = await db.Stores.FindAsync(storeId)
            ?? throw new KeyNotFoundException("Mağaza bulunamadı");

        var txs = await db.CreditTransactions
            .Where(c => c.StoreId == storeId && c.WholesalerId == wholesalerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var debits   = txs.Where(t => t.Type != CreditTransactionType.Payment).ToList();
        var payments = txs.Where(t => t.Type == CreditTransactionType.Payment).ToList();

        var now = DateTime.UtcNow;
        var overdueDebits = debits
            .Where(t => t.DueDate.HasValue && t.DueDate < now && !t.IsFullyAllocated)
            .ToList();

        var totalDebt    = debits.Sum(t => t.Amount);
        var totalPayment = payments.Sum(t => t.Amount);

        return new CreditSummaryDto
        {
            StoreId = storeId,
            StoreName = store.StoreName,
            TotalDebt = totalDebt,
            TotalPayment = totalPayment,
            Balance = totalDebt - totalPayment,
            OpenDebtCount = debits.Count(t => !t.IsFullyAllocated),
            OverdueAmount = overdueDebits.Sum(t => t.Amount - t.AllocatedAmount),
            OldestOverdueDate = overdueDebits.Count > 0
                ? overdueDebits.MinBy(t => t.DueDate)!.DueDate
                : null,
            UnallocatedPaymentAmount = payments.Sum(t => t.Amount - t.AllocatedAmount),
            Transactions = txs.Select(ToDto).ToList(),
        };
    }

    // ─── Ekstre ──────────────────────────────────────────────────────────────

    public async Task<CreditStatementDto> GetStatementAsync(
        Guid storeId,
        Guid wholesalerId,
        DateTime from,
        DateTime to)
    {
        // Opening balance: from öncesi tüm tx'lerin net etkisi
        var beforeTxs = await db.CreditTransactions
            .Where(c => c.StoreId == storeId && c.WholesalerId == wholesalerId && c.CreatedAt < from)
            .ToListAsync();

        decimal openingBalance = beforeTxs.Sum(c =>
            c.Type == CreditTransactionType.Payment ? -c.Amount : c.Amount);

        // Dönem içi hareketler
        var periodTxs = await db.CreditTransactions
            .Where(c =>
                c.StoreId == storeId &&
                c.WholesalerId == wholesalerId &&
                c.CreatedAt >= from &&
                c.CreatedAt <= to)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var entries = new List<CreditStatementEntryDto>();
        decimal runningBalance = openingBalance;

        foreach (var tx in periodTxs)
        {
            bool isPayment = tx.Type == CreditTransactionType.Payment;
            runningBalance += isPayment ? -tx.Amount : tx.Amount;

            entries.Add(new CreditStatementEntryDto
            {
                Date = tx.CreatedAt,
                Type = tx.Type.ToString(),
                Amount = tx.Amount,
                RemainingAmount = isPayment ? null : tx.Amount - tx.AllocatedAmount,
                RunningBalance = runningBalance,
                Description = tx.Description,
                OrderId = tx.OrderId,
            });
        }

        return new CreditStatementDto
        {
            StoreId = storeId,
            WholesalerId = wholesalerId,
            From = from,
            To = to,
            OpeningBalance = openingBalance,
            ClosingBalance = runningBalance,
            Entries = entries,
        };
    }

    // ─── Admin: Manuel Allocation ─────────────────────────────────────────────

    public async Task<PaymentAllocationDto> CreateManualAllocationAsync(
        Guid paymentTransactionId,
        Guid debitTransactionId,
        decimal amount,
        Guid adminUserId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var payment = await db.CreditTransactions.FindAsync(paymentTransactionId)
            ?? throw new KeyNotFoundException("Payment transaction bulunamadı");

        if (payment.Type != CreditTransactionType.Payment)
            throw new InvalidOperationException("Kaynak transaction Payment tipinde olmalı");

        var debit = await db.CreditTransactions.FindAsync(debitTransactionId)
            ?? throw new KeyNotFoundException("Debit transaction bulunamadı");

        if (debit.Type == CreditTransactionType.Payment)
            throw new InvalidOperationException("Hedef transaction Debit tipinde olmalı");

        // Taşma kontrolü
        decimal paymentAvailable = payment.Amount - payment.AllocatedAmount;
        decimal debitAvailable   = debit.Amount - debit.AllocatedAmount;

        if (amount > paymentAvailable)
            throw new InvalidOperationException(
                $"Payment'in kalan kapasitesi ({paymentAvailable:N2}) yetersiz");

        if (amount > debitAvailable)
            throw new InvalidOperationException(
                $"Debit'in kalan tutarı ({debitAvailable:N2}) yetersiz");

        var allocation = new PaymentAllocation
        {
            PaymentTransactionId = paymentTransactionId,
            DebitTransactionId = debitTransactionId,
            AllocatedAmount = amount,
        };
        db.PaymentAllocations.Add(allocation);

        payment.AllocatedAmount += amount;
        payment.IsFullyAllocated = payment.AllocatedAmount >= payment.Amount;

        debit.AllocatedAmount += amount;
        debit.IsFullyAllocated = debit.AllocatedAmount >= debit.Amount;

        auditService.LogAction(
            adminUserId,
            "Admin",
            "ManualAllocationCreated",
            "PaymentAllocation",
            allocation.Id.ToString(),
            new { paymentTransactionId, debitTransactionId, amount },
            ipAddress: null);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return ToAllocationDto(allocation);
    }

    public async Task DeleteAllocationAsync(Guid allocationId, Guid adminUserId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var allocation = await db.PaymentAllocations
            .Include(pa => pa.PaymentTransaction)
            .Include(pa => pa.DebitTransaction)
            .FirstOrDefaultAsync(pa => pa.Id == allocationId)
            ?? throw new KeyNotFoundException("Allocation bulunamadı");

        var payment = allocation.PaymentTransaction;
        var debit   = allocation.DebitTransaction;

        payment.AllocatedAmount -= allocation.AllocatedAmount;
        payment.IsFullyAllocated = payment.AllocatedAmount >= payment.Amount;

        debit.AllocatedAmount -= allocation.AllocatedAmount;
        debit.IsFullyAllocated = debit.AllocatedAmount >= debit.Amount;

        db.PaymentAllocations.Remove(allocation);

        auditService.LogAction(
            adminUserId,
            "Admin",
            "ManualAllocationDeleted",
            "PaymentAllocation",
            allocationId.ToString(),
            new
            {
                paymentTransactionId = allocation.PaymentTransactionId,
                debitTransactionId   = allocation.DebitTransactionId,
                amount               = allocation.AllocatedAmount,
            },
            ipAddress: null);

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    public static CreditTransactionDto ToDto(CreditTransaction t) => new()
    {
        Id = t.Id,
        Type = t.Type.ToString(),
        Amount = t.Amount,
        AllocatedAmount = t.AllocatedAmount,
        RemainingAmount = t.Amount - t.AllocatedAmount,
        IsFullyAllocated = t.IsFullyAllocated,
        Description = t.Description,
        DueDate = t.DueDate,
        OrderId = t.OrderId,
        CreatedAt = t.CreatedAt,
    };

    private static PaymentAllocationDto ToAllocationDto(PaymentAllocation pa) => new()
    {
        Id = pa.Id,
        PaymentTransactionId = pa.PaymentTransactionId,
        DebitTransactionId   = pa.DebitTransactionId,
        AllocatedAmount      = pa.AllocatedAmount,
        CreatedAt            = pa.CreatedAt,
    };
}
