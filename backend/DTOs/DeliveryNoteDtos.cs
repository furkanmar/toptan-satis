namespace WholesaleApi.DTOs;

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public class CreateDeliveryNoteDto
{
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
    public string? SourceAddress { get; set; }
    public string? DestinationAddress { get; set; }
    public string? Notes { get; set; }

    // Gönderilecek miktarlar: ProductId → qty (kısmi sevkiyat desteği)
    public Dictionary<Guid, int>? ShippedQuantities { get; set; }
}

public class CancelDeliveryNoteDto
{
    public string? CancelReason { get; set; }
}

// ─── Response DTOs ────────────────────────────────────────────────────────────

public class DeliveryNoteDto
{
    public Guid Id { get; set; }
    public string NoteNumber { get; set; } = null!;
    public Guid OrderId { get; set; }
    public Guid WholesalerId { get; set; }
    public string WholesalerName { get; set; } = null!;
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
    public string? SourceAddress { get; set; }
    public string? DestinationAddress { get; set; }
    public string? Notes { get; set; }
    public string? Ettn { get; set; }
    public string? EInvoiceProvider { get; set; }
    public string? EInvoiceStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public List<DeliveryNoteItemDto> Items { get; set; } = [];
}

public class DeliveryNoteItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ProductBrand { get; set; }
    public string UnitType { get; set; } = null!;
    public int ContentQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public int QuantityOrdered { get; set; }
    public int QuantityShipped { get; set; }
    public decimal LineTotal { get; set; }
}

public class DeliveryNoteListItemDto
{
    public Guid Id { get; set; }
    public string NoteNumber { get; set; } = null!;
    public Guid OrderId { get; set; }
    public string StoreName { get; set; } = null!;
    public string WholesalerName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
}
