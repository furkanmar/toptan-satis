namespace WholesaleApi.Entities;

public enum DeliveryNoteStatus
{
    Draft,
    Issued,
    Cancelled
}

public class DeliveryNote
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Auto-generated: SI-YYYYMMDD-NNNN
    public string NoteNumber { get; set; } = null!;

    public Guid OrderId { get; set; }
    public Guid WholesalerId { get; set; }
    public Guid StoreId { get; set; }

    public DeliveryNoteStatus Status { get; set; } = DeliveryNoteStatus.Draft;

    public DateTime IssueDate { get; set; }

    // Taşıma bilgileri
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
    public string? SourceAddress { get; set; }
    public string? DestinationAddress { get; set; }
    public string? Notes { get; set; }

    // e-İrsaliye placeholder alanları
    public string? Ettn { get; set; }
    public string? EInvoiceProvider { get; set; }
    public string? EInvoiceStatus { get; set; }
    public string? EInvoiceRawResponse { get; set; }  // jsonb

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public Wholesaler Wholesaler { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public ICollection<DeliveryNoteItem> Items { get; set; } = [];
}
