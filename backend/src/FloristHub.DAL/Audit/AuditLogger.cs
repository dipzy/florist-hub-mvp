using FloristHub.DAL.Entities;
using FloristHub.DAL.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace FloristHub.DAL.Audit;

public class AuditLogger : IAuditLogger
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(IUnitOfWork unitOfWork, ILogger<AuditLogger> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogAsync(string action, Guid? userId, Guid? tenantId, string? entityType = null, Guid? entityId = null, string? details = null, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AUDIT {Action} User={UserId} Tenant={TenantId} Entity={EntityType}/{EntityId} {Details}",
            action, userId, tenantId, entityType, entityId, details);

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Action = action,
            UserId = userId,
            TenantId = tenantId,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
