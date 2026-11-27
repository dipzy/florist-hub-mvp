namespace FloristHub.Services.Payments;

/// <summary>
/// Simula o Checkout em desenvolvimento. Não fala com o Stripe e não aceita webhooks externos.
/// </summary>
public sealed class DevelopmentStripeGateway : IStripeGateway
{
    public bool IsConfigured => true;

    public bool SimulatesCheckout => true;

    public Task<ExpressAccountResult> CreateExpressAccountAsync(CreateExpressAccountCommand command, CancellationToken ct = default) =>
        Task.FromResult(new ExpressAccountResult("acct_dev_fake"));

    public Task<string> CreateAccountLinkAsync(AccountLinkCommand command, CancellationToken ct = default) =>
        Task.FromResult(command.ReturnUrl);

    public Task<ConnectedAccountSnapshot> GetAccountAsync(string accountId, CancellationToken ct = default) =>
        Task.FromResult(new ConnectedAccountSnapshot(accountId, true, true, true, true, true, true));

    public Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutCommand command, CancellationToken ct = default)
    {
        var id = command.PaymentId.ToString("N");
        return Task.FromResult(new CheckoutSessionResult("cs_fake_" + id, command.SuccessUrl, "pi_fake_" + id[..12], true));
    }

    public Task<RefundResult> CreateRefundAsync(CreateRefundCommand command, CancellationToken ct = default) =>
        Task.FromResult(new RefundResult("re_fake", command.AmountCents ?? 0));

    public StripeWebhookNotification ParseWebhook(string payload, string signatureHeader) =>
        throw new StripeSignatureException("O modo de teste local não aceita webhooks.");
}
