namespace FloristHub.Services.Payments;

public sealed class DisabledStripeGateway : IStripeGateway
{
    public bool IsConfigured => false;

    public bool SimulatesCheckout => false;

    public Task<ExpressAccountResult> CreateExpressAccountAsync(CreateExpressAccountCommand command, CancellationToken ct = default) =>
        throw Disabled();

    public Task<string> CreateAccountLinkAsync(AccountLinkCommand command, CancellationToken ct = default) =>
        throw Disabled();

    public Task<ConnectedAccountSnapshot> GetAccountAsync(string accountId, CancellationToken ct = default) =>
        throw Disabled();

    public Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutCommand command, CancellationToken ct = default) =>
        throw Disabled();

    public Task<RefundResult> CreateRefundAsync(CreateRefundCommand command, CancellationToken ct = default) =>
        throw Disabled();

    public StripeWebhookNotification ParseWebhook(string payload, string signatureHeader) =>
        throw Disabled();

    private static InvalidOperationException Disabled() =>
        new("Pagamentos Stripe desativados.");
}
