namespace FloristHub.Services.Payments;

public abstract record StripeWebhookNotification(string EventId, string Type);

public sealed record CheckoutSessionCompletedNotification(
    string EventId,
    string SessionId,
    string? PaymentIntentId,
    bool Paid,
    IReadOnlyDictionary<string, string> Metadata) : StripeWebhookNotification(EventId, "checkout.session.completed");

public sealed record AsyncPaymentSucceededNotification(
    string EventId,
    string SessionId,
    string? PaymentIntentId,
    IReadOnlyDictionary<string, string> Metadata) : StripeWebhookNotification(EventId, "checkout.session.async_payment_succeeded");

public sealed record AsyncPaymentFailedNotification(
    string EventId,
    string SessionId,
    string? PaymentIntentId,
    IReadOnlyDictionary<string, string> Metadata) : StripeWebhookNotification(EventId, "checkout.session.async_payment_failed");

public sealed record PaymentIntentFailedNotification(
    string EventId,
    string PaymentIntentId,
    string? Message,
    IReadOnlyDictionary<string, string> Metadata) : StripeWebhookNotification(EventId, "payment_intent.payment_failed");

public sealed record ChargeRefundedNotification(
    string EventId,
    string? PaymentIntentId,
    string? ChargeId,
    long AmountRefundedCents,
    bool FullyRefunded) : StripeWebhookNotification(EventId, "charge.refunded");

public sealed record AccountUpdatedNotification(
    string EventId,
    string AccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted,
    bool CardPayments,
    bool MbWay,
    bool Multibanco) : StripeWebhookNotification(EventId, "account.updated");

public sealed record IgnoredStripeNotification(string EventId, string Type) : StripeWebhookNotification(EventId, Type);
