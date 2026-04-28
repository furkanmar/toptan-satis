namespace WholesaleApi.Entities;

public class IdempotencyKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = null!;           // Idempotency-Key header değeri
    public Guid UserId { get; set; }
    public string RequestPath { get; set; } = null!;
    public int ResponseStatusCode { get; set; }
    public string ResponseBody { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}
