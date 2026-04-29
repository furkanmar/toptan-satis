namespace WholesaleApi.Entities;

public class DeliveryNoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeliveryNoteId { get; set; }
    public Guid ProductId { get; set; }

    // Snapshot alanları (sipariş anındaki değerler)
    public string ProductName { get; set; } = null!;
    public string? ProductBrand { get; set; }
    public string UnitType { get; set; } = null!;
    public int ContentQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }

    public int QuantityOrdered { get; set; }
    public int QuantityShipped { get; set; }

    public decimal LineTotal => UnitPrice * QuantityShipped;

    // Navigation
    public DeliveryNote DeliveryNote { get; set; } = null!;
}
