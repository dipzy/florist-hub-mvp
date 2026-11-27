using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Common;
using FloristHub.Services.DTOs.Legal;
using FloristHub.Services.Legal;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services;

public interface IComplianceAuditService
{
    Task<PagedResult<ComplianceAuditEntryDto>> ListForFloristAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<ComplianceAuditEntryDto>> ListForSuperAdminAsync(int page, int pageSize, Guid? tenantId, CancellationToken ct = default);
}

public class ComplianceAuditService : IComplianceAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;

    public ComplianceAuditService(IUnitOfWork unitOfWork, ITenantContext tenant)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
    }

    public async Task<PagedResult<ComplianceAuditEntryDto>> ListForFloristAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");
        return await QueryAsync(tenantId, page, pageSize, ct);
    }

    public Task<PagedResult<ComplianceAuditEntryDto>> ListForSuperAdminAsync(
        int page,
        int pageSize,
        Guid? tenantId,
        CancellationToken ct = default) =>
        QueryAsync(tenantId, page, pageSize, ct);

    private async Task<PagedResult<ComplianceAuditEntryDto>> QueryAsync(
        Guid? tenantFilter,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _unitOfWork.AuditLogs.Query().AsNoTracking()
            .Where(a => ComplianceAuditActions.All.Contains(a.Action));

        if (tenantFilter is Guid tid)
            query = query.Where(a => a.TenantId == tid);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.OccurredAt,
                a.TenantId,
                a.UserId,
                a.Action,
                a.Details,
            })
            .ToListAsync(ct);

        var tenantIds = rows.Select(r => r.TenantId).Where(id => id is Guid).Cast<Guid>().Distinct().ToList();
        var tenantNames = tenantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _unitOfWork.Tenants.Query().AsNoTracking()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var mapped = rows.Select(a => new ComplianceAuditEntryDto(
            a.Id,
            a.OccurredAt,
            a.TenantId,
            a.TenantId is Guid tid && tenantNames.TryGetValue(tid, out var name) ? name : null,
            a.UserId,
            a.Action,
            a.Details)).ToList();

        return new PagedResult<ComplianceAuditEntryDto>(mapped, total, page, pageSize);
    }
}
