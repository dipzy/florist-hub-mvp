using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class NotificationPreference : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public bool NewOrder { get; set; } = true;
    public bool OrderPaid { get; set; } = true;
    public bool OrderStatus { get; set; } = true;
    public bool LowStock { get; set; } = true;
    public bool RecurringSubscriptions { get; set; } = true;
}
