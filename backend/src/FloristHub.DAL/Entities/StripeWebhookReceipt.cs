using FloristHub.DAL.Common;

namespace FloristHub.DAL.Entities;

/// <summary>Registo de eventos Stripe já processados, para idempotência.</summary>
public class StripeWebhookReceipt : BaseEntity
{
    public required string StripeEventId { get; set; }
    public required string EventType { get; set; }
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
