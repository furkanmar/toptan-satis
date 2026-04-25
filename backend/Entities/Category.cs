namespace WholesaleApi.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    // Navigation
    public ICollection<Product> Products { get; set; } = [];
}
