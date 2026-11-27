using FloristHub.DAL.Entities;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;

namespace FloristHub.Services.Onboarding;

public static class TenantPublicAccessRules
{
    public static void EnsurePublicStoreOpen(Tenant tenant)
    {
        if (!tenant.IsActive)
            throw new InvalidOperationException("Esta loja está suspensa.");

        if (!tenant.IsPublished)
            throw new InvalidOperationException("Esta loja ainda não está publicada.");
    }

    public static async Task<Tenant> RequirePublishedTenantAsync(
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException("Loja não resolvida.");
        var tenant = await unitOfWork.Tenants.GetByIdReadOnlyAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Loja não encontrada.");
        EnsurePublicStoreOpen(tenant);
        return tenant;
    }
}
