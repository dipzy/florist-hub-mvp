using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

public class RecurringSubscriptionItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid RecurringSubscriptionId { get; set; }
    public RecurringSubscription? RecurringSubscription { get; set; }

    public Guid? FlowerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
