using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class DeliveryZone : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public decimal Fee { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
