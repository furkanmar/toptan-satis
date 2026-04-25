namespace WholesaleApi.Entities;

public enum OrderStatus { Pending, Confirmed, Rejected, Delivered, Cancelled }

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }
    public Guid WholesalerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Store Store { get; set; } = null!;
    public Wholesaler Wholesaler { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
}
