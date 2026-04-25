namespace WholesaleApi.Entities;

public class Wholesaler
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<StoreWholesaler> StoreWholesalers { get; set; } = [];
    public ICollection<CreditTransaction> CreditTransactions { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
}
