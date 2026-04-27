using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WholesaleApi.Entities;

namespace WholesaleApi.Data;

/// <summary>
/// [Auditable] entity'lerin Modified değişikliklerini otomatik olarak AuditLog'a yazar.
/// Aynı SaveChanges transaction'ına dahil edilir — rollback olursa log da rollback olur.
/// Singleton olarak kayıtlıdır; IHttpContextAccessor AsyncLocal sayesinde thread-safe'dir.
/// </summary>
public class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    // Gürültü oluşturan, anlamsız değişiklik alanları
    private static readonly HashSet<string> IgnoredProperties =
        ["xmin", "CreatedAt", "UpdatedAt", "PasswordHash"];

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var entries = BuildAuditEntries(eventData.Context);
            if (entries.Count > 0)
                eventData.Context.Set<AuditLog>().AddRange(entries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditLog> BuildAuditEntries(DbContext context)
    {
        var httpCtx = httpContextAccessor.HttpContext;
        var userId = ParseUserId(httpCtx);
        var userRole = httpCtx?.User.FindFirstValue(ClaimTypes.Role) ?? "System";
        var ip = httpCtx?.Connection.RemoteIpAddress?.ToString();

        var logs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Sadece [Auditable] + Modified
            if (entry.State != EntityState.Modified) continue;
            if (entry.Entity.GetType().GetCustomAttributes(typeof(AuditableAttribute), false).Length == 0)
                continue;

            var before = new Dictionary<string, object?>();
            var after = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                if (!prop.IsModified) continue;
                if (IgnoredProperties.Contains(prop.Metadata.Name)) continue;

                before[prop.Metadata.Name] = prop.OriginalValue;
                after[prop.Metadata.Name] = prop.CurrentValue;
            }

            if (before.Count == 0) continue;

            // Primary key — genellikle ilk PK property
            var entityId = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())
                ?.CurrentValue?.ToString() ?? "";

            var entityType = entry.Entity.GetType().Name;

            logs.Add(new AuditLog
            {
                UserId = userId,
                UserRole = userRole,
                Action = $"{entityType}Updated",
                EntityType = entityType,
                EntityId = entityId,
                Changes = JsonSerializer.Serialize(new { before, after }, JsonOpts),
                IpAddress = ip,
            });
        }

        return logs;
    }

    private static Guid? ParseUserId(HttpContext? ctx)
    {
        var raw = ctx?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
