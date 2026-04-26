namespace WholesaleApi.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = null!;
    public Guid WholesalerId { get; set; }
    public string WholesalerName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public string? WholesalerNote { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public string UnitType { get; set; } = "Adet";
    public int ContentQty { get; set; } = 1;
    public int VatRate { get; set; } = 18;
}

public class CreateOrderDto
{
    public string? Note { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = [];
}

public class CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public Guid? UnitConfigId { get; set; }  // null → ilk/varsayılan birim tipi
    public int Quantity { get; set; }
}

public class ConfirmOrderDto
{
    public string? WholesalerNote { get; set; }
    public DateTime? DueDate { get; set; }
    public bool CreateCreditEntry { get; set; } = true;
    public bool ForceConfirm { get; set; } = false;
}

public class UpdateOrderItemsDto
{
    public List<CreateOrderItemDto> Items { get; set; } = [];
    public string? WholesalerNote { get; set; }
}

public record UpdateOrderStatusDto(string Status);
