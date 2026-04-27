namespace WholesaleApi.DTOs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string UserRole { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? Changes { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogPageDto
{
    public List<AuditLogDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
