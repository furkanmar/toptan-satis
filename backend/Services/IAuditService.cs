namespace WholesaleApi.Services;

public interface IAuditService
{
    /// <summary>
    /// İş aksiyonu kaydı oluşturur. SaveChanges ÇAĞIRMAZ — caller transaction içinde save eder.
    /// </summary>
    void LogAction(
        Guid? userId,
        string userRole,
        string action,
        string entityType,
        string entityId,
        object? payload = null,
        string? ipAddress = null);
}
