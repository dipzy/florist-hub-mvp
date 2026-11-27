using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

public class PaymentEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public required string EventType { get; set; }
    public string? StripeEventId { get; set; }
    public PaymentStatus? FromStatus { get; set; }
    public PaymentStatus ToStatus { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }
}
