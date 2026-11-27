using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class TenantOpeningHour : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public bool IsClosed { get; set; }
}
