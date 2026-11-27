using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

public class RecurringOccurrence : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid RecurringSubscriptionId { get; set; }
    public RecurringSubscription? RecurringSubscription { get; set; }

    public DateTimeOffset ScheduledDeliveryAt { get; set; }
    public RecurringOccurrenceStatus Status { get; set; } = RecurringOccurrenceStatus.Scheduled;

    public Guid? GeneratedOrderId { get; set; }
    public Order? GeneratedOrder { get; set; }

    /// <summary>Chave de idempotência: subscrição + instante de entrega.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;
}
