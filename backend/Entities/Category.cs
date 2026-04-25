namespace WholesaleApi.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? WholesalerId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    // Navigation
    public Wholesaler? Wholesaler { get; set; }
    public ICollection<Product> Products { get; set; } = [];
}
