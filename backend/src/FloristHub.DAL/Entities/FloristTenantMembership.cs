using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

/// <summary>Ligação florista ↔ loja (um utilizador pode gerir várias lojas).</summary>
public class FloristTenantMembership : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }

    public ApplicationUser? User { get; set; }
    public Tenant? Tenant { get; set; }
}
