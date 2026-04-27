using System.Text.Json;
using WholesaleApi.Data;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class AuditService(AppDbContext db) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public void LogAction(
        Guid? userId,
        string userRole,
        string action,
        string entityType,
        string entityId,
        object? payload = null,
        string? ipAddress = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            UserRole = userRole,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Changes = payload != null ? JsonSerializer.Serialize(payload, JsonOpts) : null,
            IpAddress = ipAddress,
        });
    }
}
