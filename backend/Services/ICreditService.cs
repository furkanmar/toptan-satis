using WholesaleApi.DTOs;

namespace WholesaleApi.Services;

public interface ICreditService
{
    /// <summary>
    /// Sipariş onaylanınca çağrılır. Çağıran zaten bir DB transaction içindeyse
    /// bu metot ayrı transaction AÇMAZ — çağıranın tx'ine katılır.
    /// </summary>
    Task RecordOrderDebitAsync(
        Guid storeId,
        Guid wholesalerId,
        decimal amount,
        string description,
        DateTime? dueDate,
        Guid orderId);

    /// <summary>
    /// Manuel borç kaydı. Ayrı transaction açmaz.
    /// </summary>
    Task ManualDebitAsync(
        Guid storeId,
        Guid wholesalerId,
        decimal amount,
        string description,
        DateTime? dueDate);

    /// <summary>
    /// Ödeme kaydeder ve FIFO ile açık debit'lere dağıtır.
    /// DbUpdateConcurrencyException durumunda 3 deneme yapar.
    /// </summary>
    Task<PaymentResult> RecordPaymentAsync(
        Guid storeId,
        Guid wholesalerId,
        decimal amount,
        string description);

    Task<CreditSummaryDto> GetSummaryAsync(Guid storeId, Guid wholesalerId);

    Task<CreditStatementDto> GetStatementAsync(
        Guid storeId,
        Guid wholesalerId,
        DateTime from,
        DateTime to);

    /// <summary>Admin: FIFO dışı manuel allocation oluştur. AuditLog'a düşer.</summary>
    Task<PaymentAllocationDto> CreateManualAllocationAsync(
        Guid paymentTransactionId,
        Guid debitTransactionId,
        decimal amount,
        Guid adminUserId);

    /// <summary>
    /// Admin: Bir allocation'ı iptal et.
    /// Debit ve Payment AllocatedAmount'larını geri hesaplar. AuditLog'a düşer.
    /// </summary>
    Task DeleteAllocationAsync(Guid allocationId, Guid adminUserId);
}
