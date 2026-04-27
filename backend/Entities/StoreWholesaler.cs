namespace WholesaleApi.Entities;

[Auditable]
public class StoreWholesaler
{
    public Guid StoreId { get; set; }
    public Guid WholesalerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Store Store { get; set; } = null!;
    public Wholesaler Wholesaler { get; set; } = null!;
}
