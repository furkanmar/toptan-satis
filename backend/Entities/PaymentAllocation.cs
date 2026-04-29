namespace WholesaleApi.Entities;

/// <summary>
/// Bir ödemenin hangi borç(lar)ı kapattığını tutan köprü kayıt.
/// Append-only — asla UPDATE/DELETE yapılmaz (admin override hariç).
/// </summary>
public class PaymentAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Payment tipindeki CreditTransaction.Id</summary>
    public Guid PaymentTransactionId { get; set; }

    /// <summary>OrderDebit veya ManualDebit tipindeki CreditTransaction.Id</summary>
    public Guid DebitTransactionId { get; set; }

    public decimal AllocatedAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CreditTransaction PaymentTransaction { get; set; } = null!;
    public CreditTransaction DebitTransaction { get; set; } = null!;
}
