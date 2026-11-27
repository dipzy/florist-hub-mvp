namespace FloristHub.DAL.MultiTenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? TenantSlug { get; }
    void SetTenant(Guid tenantId, string? slug = null);
}
