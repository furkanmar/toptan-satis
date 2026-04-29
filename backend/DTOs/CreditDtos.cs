namespace WholesaleApi.DTOs;

public class CreditTransactionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsFullyAllocated { get; set; }
    public string Description { get; set; } = null!;
    public DateTime? DueDate { get; set; }
    public Guid? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreditSummaryDto
{
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = null!;

    /// <summary>Toplam borç (OrderDebit + ManualDebit).</summary>
    public decimal TotalDebt { get; set; }

    /// <summary>Toplam ödeme.</summary>
    public decimal TotalPayment { get; set; }

    /// <summary>Net bakiye = TotalDebt - TotalPayment.</summary>
    public decimal Balance { get; set; }

    /// <summary>RemainingAmount > 0 olan debit sayısı.</summary>
    public int OpenDebtCount { get; set; }

    /// <summary>Vadesi geçmiş ve hâlâ açık debit'lerin RemainingAmount toplamı.</summary>
    public decimal OverdueAmount { get; set; }

    /// <summary>En eski vadesi geçmiş debit'in DueDate'i.</summary>
    public DateTime? OldestOverdueDate { get; set; }

    /// <summary>Ödeme avansı — Payment'lerden borçlara dağıtılmamış kalan.</summary>
    public decimal UnallocatedPaymentAmount { get; set; }

    public List<CreditTransactionDto> Transactions { get; set; } = [];
}

/// <summary>Tarih filtreli ekstre satırı.</summary>
public class CreditStatementEntryDto
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = null!;
    public decimal Amount { get; set; }

    /// <summary>Debit için kalan açık tutar; Payment için null.</summary>
    public decimal? RemainingAmount { get; set; }

    public decimal RunningBalance { get; set; }
    public string Description { get; set; } = null!;
    public Guid? OrderId { get; set; }
}

/// <summary>Ekstre endpoint'inin tüm yanıtı.</summary>
public class CreditStatementDto
{
    public Guid StoreId { get; set; }
    public Guid WholesalerId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    /// <summary>From tarihinden önceki net bakiye (borç - ödeme).</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>Son hareketin RunningBalance değeri.</summary>
    public decimal ClosingBalance { get; set; }

    public List<CreditStatementEntryDto> Entries { get; set; } = [];
}

public class CreateCreditTransactionDto
{
    public Guid StoreId { get; set; }

    /// <summary>ManualDebit veya Payment.</summary>
    public string Type { get; set; } = "ManualDebit";
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public DateTime? DueDate { get; set; }
}

public class PaymentResult
{
    public Guid TransactionId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AllocatedAmount { get; set; }

    /// <summary>Borçlara dağıtılamayan avans miktarı.</summary>
    public decimal UnallocatedAmount { get; set; }
}

public class PaymentAllocationDto
{
    public Guid Id { get; set; }
    public Guid PaymentTransactionId { get; set; }
    public Guid DebitTransactionId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateManualAllocationDto
{
    public Guid PaymentTransactionId { get; set; }
    public Guid DebitTransactionId { get; set; }
    public decimal Amount { get; set; }
}
