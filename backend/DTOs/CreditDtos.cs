namespace WholesaleApi.DTOs;

public class CreditTransactionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public DateTime? DueDate { get; set; }
    public Guid? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreditSummaryDto
{
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = null!;
    public decimal TotalDebt { get; set; }      // Toplam borç
    public decimal TotalPaid { get; set; }      // Toplam ödeme
    public decimal Balance { get; set; }         // Kalan bakiye (borç - ödeme)
    public decimal OverdueAmount { get; set; }  // Vadesi geçmiş
    public List<CreditTransactionDto> Transactions { get; set; } = [];
}

public class CreateCreditTransactionDto
{
    public Guid StoreId { get; set; }
    public string Type { get; set; } = "ManualDebit"; // ManualDebit | Payment
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public DateTime? DueDate { get; set; }
}
