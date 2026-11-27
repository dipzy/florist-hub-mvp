using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class DeliveryCapacityPolicy : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public int DailyOrderLimit { get; set; }
}
