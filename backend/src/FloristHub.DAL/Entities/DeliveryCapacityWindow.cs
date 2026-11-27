using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class DeliveryCapacityWindow : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public required string Label { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int OrderLimit { get; set; }
    public int SortOrder { get; set; }

    public string Key => $"{StartTime:HH:mm}-{EndTime:HH:mm}";
}
