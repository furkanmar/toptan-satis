namespace WholesaleApi.Entities;

public enum OrderStatus { Pending, Confirmed, Rejected, Delivered, Cancelled }

[Auditable]
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }
    public Guid WholesalerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }           // Mağazanın notu
    public string? WholesalerNote { get; set; } // Toptancının notu
    public DateTime? DueDate { get; set; }      // Vade tarihi (onaylarken girilir)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Store Store { get; set; } = null!;
    public Wholesaler Wholesaler { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<CreditTransaction> CreditTransactions { get; set; } = [];
}
