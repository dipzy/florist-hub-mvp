namespace FloristHub.DAL.Audit;

public interface IAuditLogger
{
    Task LogAsync(string action, Guid? userId, Guid? tenantId, string? entityType = null, Guid? entityId = null, string? details = null, CancellationToken ct = default);
}
