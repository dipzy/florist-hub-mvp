namespace FloristHub.Services.Payments;

public interface IStripeGateway
{
    bool IsConfigured { get; }

    /// <summary>Checkout local, sem chamada ao Stripe. Só para desenvolvimento.</summary>
    bool SimulatesCheckout => false;

    Task<ExpressAccountResult> CreateExpressAccountAsync(CreateExpressAccountCommand command, CancellationToken ct = default);

    Task<string> CreateAccountLinkAsync(AccountLinkCommand command, CancellationToken ct = default);

    Task<ConnectedAccountSnapshot> GetAccountAsync(string accountId, CancellationToken ct = default);

    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutCommand command, CancellationToken ct = default);

    Task<RefundResult> CreateRefundAsync(CreateRefundCommand command, CancellationToken ct = default);

    StripeWebhookNotification ParseWebhook(string payload, string signatureHeader);
}

public sealed record CreateExpressAccountCommand(string? Email, Guid TenantId, string BusinessName);

public sealed record ExpressAccountResult(string AccountId);

public sealed record AccountLinkCommand(string AccountId, string RefreshUrl, string ReturnUrl);

public sealed record ConnectedAccountSnapshot(
    string AccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted,
    bool CardPayments,
    bool MbWay,
    bool Multibanco);

public sealed record CreateCheckoutCommand(
    string ConnectedAccountId,
    long AmountCents,
    long? ApplicationFeeCents,
    string SuccessUrl,
    string CancelUrl,
    string ProductName,
    string? CustomerEmail,
    Guid OrderId,
    Guid PaymentId,
    Guid TenantId,
    bool Card,
    bool MbWay,
    bool Multibanco);

public sealed record CheckoutSessionResult(string SessionId, string Url, string? PaymentIntentId, bool SettleImmediately = false);

public sealed record CreateRefundCommand(
    string ConnectedAccountId,
    string PaymentIntentId,
    long? AmountCents,
    bool RefundApplicationFee);

public sealed record RefundResult(string RefundId, long AmountCents);
