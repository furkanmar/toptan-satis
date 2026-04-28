namespace WholesaleApi.DTOs;

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string MovementType { get; set; } = null!;
    public int QuantityChange { get; set; }
    public int BalanceAfter { get; set; }
    public Guid? OrderId { get; set; }
    public Guid UserId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StockMovementPageDto
{
    public List<StockMovementDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class StockAdjustmentRequestDto
{
    public int NewStock { get; set; }
    public string Reason { get; set; } = null!;
}

public class NotificationLogDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public string Channel { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationLogPageDto
{
    public List<NotificationLogDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
