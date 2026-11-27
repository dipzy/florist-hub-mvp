using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

public class Payment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "eur";
    public decimal CommissionPercent { get; set; }
    public long ApplicationFeeAmountCents { get; set; }
    public decimal RefundedAmount { get; set; }

    public string? StripeCheckoutSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeChargeId { get; set; }
    public string? StripeConnectedAccountId { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? FailureMessage { get; set; }

    public ICollection<PaymentEvent> Events { get; set; } = new List<PaymentEvent>();
}
