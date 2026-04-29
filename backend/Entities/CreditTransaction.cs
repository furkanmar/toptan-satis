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

    /// <summary>Her zaman pozitif brüt tutar.</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Kapanan/dağıtılan tutar.
    /// Debit için: bu borçtan ödenen toplam.
    /// Payment için: bu ödemeden borçlara dağıtılan toplam.
    /// Default 0; migration backfill ile mevcut kayıtlar 0 olarak kalır.
    /// </summary>
    public decimal AllocatedAmount { get; set; } = 0;

    /// <summary>AllocatedAmount >= Amount olduğunda true. Servis katmanı günceller.</summary>
    public bool IsFullyAllocated { get; set; } = false;

    /// <summary>Kalan tutar — EF'e map edilmez, hesaplanır.</summary>
    public decimal RemainingAmount => Amount - AllocatedAmount;

    public string Description { get; set; } = null!;

    /// <summary>Vade tarihi — yalnızca Debit türleri için anlamlı.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Bağlı sipariş (OrderDebit için set edilir).</summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Vade geçişi Telegram bildirimi gönderildiği zaman.
    /// Bir borç için sadece bir kez bildirim atılmasını sağlar.
    /// </summary>
    public DateTime? LastOverdueNotifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Store Store { get; set; } = null!;
    public Wholesaler Wholesaler { get; set; } = null!;
    public Order? Order { get; set; }

    /// <summary>Bu işlemin ödeme tarafındaki allocation kayıtları.</summary>
    public ICollection<PaymentAllocation> PaymentAllocations { get; set; } = [];

    /// <summary>Bu işlemin borç tarafındaki allocation kayıtları.</summary>
    public ICollection<PaymentAllocation> DebitAllocations { get; set; } = [];
}
