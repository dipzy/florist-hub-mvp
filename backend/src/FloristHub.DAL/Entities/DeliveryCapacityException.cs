using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class DeliveryCapacityException : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DateOnly ServiceDate { get; set; }
    public bool IsClosed { get; set; }
    public int? DailyOrderLimitOverride { get; set; }
    public string? Note { get; set; }
}
