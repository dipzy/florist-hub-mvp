using FloristHub.Services.Payments;

namespace FloristHub.Services.Tests.Payments;

public sealed class FakeStripeGateway : IStripeGateway
{
    public bool IsConfigured { get; set; } = true;
    public bool SimulatesCheckout => false;
    public bool FailSignature { get; set; }
    public StripeWebhookNotification? Notification { get; set; }
    public List<CreateCheckoutCommand> Checkouts { get; } = new();
    public List<CreateRefundCommand> Refunds { get; } = new();
    public ConnectedAccountSnapshot Account { get; set; } = new("acct_test", true, true, true, true, true, true);

    public Task<ExpressAccountResult> CreateExpressAccountAsync(CreateExpressAccountCommand command, CancellationToken ct = default) =>
        Task.FromResult(new ExpressAccountResult("acct_new"));

    public Task<string> CreateAccountLinkAsync(AccountLinkCommand command, CancellationToken ct = default) =>
        Task.FromResult("https://connect.stripe.test/setup");

    public Task<ConnectedAccountSnapshot> GetAccountAsync(string accountId, CancellationToken ct = default) =>
        Task.FromResult(Account with { AccountId = accountId });

    public Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutCommand command, CancellationToken ct = default)
    {
        Checkouts.Add(command);
        return Task.FromResult(new CheckoutSessionResult("cs_test", "https://checkout.stripe.test/cs_test", "pi_test"));
    }

    public Task<RefundResult> CreateRefundAsync(CreateRefundCommand command, CancellationToken ct = default)
    {
        Refunds.Add(command);
        return Task.FromResult(new RefundResult("re_test", command.AmountCents ?? 0));
    }

    public StripeWebhookNotification ParseWebhook(string payload, string signatureHeader)
    {
        if (FailSignature || signatureHeader == "invalid")
            throw new StripeSignatureException("Assinatura inválida.");
        return Notification ?? throw new InvalidOperationException("Evento de teste em falta.");
    }
}
