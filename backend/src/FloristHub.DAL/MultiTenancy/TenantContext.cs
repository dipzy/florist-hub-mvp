
namespace FloristHub.DAL.MultiTenancy;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public string? TenantSlug { get; private set; }

    public void SetTenant(Guid tenantId, string? slug = null)
    {
        TenantId = tenantId;
        TenantSlug = slug;
    }
}
