namespace WholesaleApi.Entities;

public enum CreditTransactionType
{
    OrderDebit,   // Sipariş → mağaza borçlandı
    ManualDebit,  // Manuel borç girişi
    Payment,      // Ödeme yapıldı
}

[Auditable]
public class CreditTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }
    public Guid WholesalerId { get; set; }
    public CreditTransactionType Type { get; set; }
    public decimal Amount { get; set; }         // Her zaman pozitif
    public string Description { get; set; } = null!;
    public DateTime? DueDate { get; set; }      // Vade tarihi
    public Guid? OrderId { get; set; }          // Bağlı sipariş (varsa)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Store Store { get; set; } = null!;
    public Wholesaler Wholesaler { get; set; } = null!;
    public Order? Order { get; set; }
}
