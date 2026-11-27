using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace FloristHub.Services.Payments;

/// <summary>
/// Adaptador Stripe.net. Os testes usam <see cref="IStripeGateway"/> falso e não esta classe, excepto a verificação de assinatura.
/// </summary>
public sealed class StripeGateway : IStripeGateway
{
    public bool SimulatesCheckout => false;

    private readonly StripeOptions _options;

    public StripeGateway(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        if (!string.IsNullOrWhiteSpace(_options.SecretKey))
            StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.SecretKey);

    public async Task<ExpressAccountResult> CreateExpressAccountAsync(CreateExpressAccountCommand command, CancellationToken ct = default)
    {
        var options = new AccountCreateOptions
        {
            Type = "express",
            Country = "PT",
            Email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim(),
            BusinessProfile = new AccountBusinessProfileOptions { Name = command.BusinessName },
            Capabilities = new AccountCapabilitiesOptions
            {
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                MbWayPayments = new AccountCapabilitiesMbWayPaymentsOptions { Requested = true },
                MultibancoPayments = new AccountCapabilitiesMultibancoPaymentsOptions { Requested = true }
            },
            Metadata = new Dictionary<string, string> { ["tenantId"] = command.TenantId.ToString() }
        };

        var account = await CallAsync(() => new AccountService().CreateAsync(options, cancellationToken: ct));
        return new ExpressAccountResult(account.Id);
    }

    public async Task<string> CreateAccountLinkAsync(AccountLinkCommand command, CancellationToken ct = default)
    {
        var link = await CallAsync(() => new AccountLinkService().CreateAsync(new AccountLinkCreateOptions
        {
            Account = command.AccountId,
            RefreshUrl = command.RefreshUrl,
            ReturnUrl = command.ReturnUrl,
            Type = "account_onboarding"
        }, cancellationToken: ct));
        return link.Url;
    }

    public async Task<ConnectedAccountSnapshot> GetAccountAsync(string accountId, CancellationToken ct = default)
    {
        var account = await CallAsync(() => new AccountService().GetAsync(accountId, cancellationToken: ct));
        return MapAccount(account);
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutCommand command, CancellationToken ct = default)
    {
        var methods = new List<string>();
        if (command.Card) methods.Add("card");
        if (command.MbWay) methods.Add("mb_way");
        if (command.Multibanco) methods.Add("multibanco");
        if (methods.Count == 0)
            throw new InvalidOperationException("A conta Stripe não tem métodos de pagamento ativos.");

        var metadata = new Dictionary<string, string>
        {
            ["orderId"] = command.OrderId.ToString(),
            ["paymentId"] = command.PaymentId.ToString(),
            ["tenantId"] = command.TenantId.ToString()
        };

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = command.SuccessUrl,
            CancelUrl = command.CancelUrl,
            CustomerEmail = string.IsNullOrWhiteSpace(command.CustomerEmail) ? null : command.CustomerEmail,
            ClientReferenceId = command.OrderId.ToString(),
            AllowedPaymentMethodTypes = methods,
            Metadata = metadata,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = command.AmountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = command.ProductName }
                    }
                }
            ],
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = command.ApplicationFeeCents is > 0 ? command.ApplicationFeeCents : null,
                Metadata = metadata
            }
        };

        var request = new RequestOptions { StripeAccount = command.ConnectedAccountId };
        var session = await CallAsync(() => new SessionService().CreateAsync(options, request, ct));
        return new CheckoutSessionResult(session.Id, session.Url, session.PaymentIntentId);
    }

    public async Task<RefundResult> CreateRefundAsync(CreateRefundCommand command, CancellationToken ct = default)
    {
        var options = new RefundCreateOptions
        {
            PaymentIntent = command.PaymentIntentId,
            Amount = command.AmountCents,
            RefundApplicationFee = command.RefundApplicationFee
        };
        var request = new RequestOptions { StripeAccount = command.ConnectedAccountId };
        var refund = await CallAsync(() => new RefundService().CreateAsync(options, request, ct));
        return new RefundResult(refund.Id, refund.Amount);
    }

    public StripeWebhookNotification ParseWebhook(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            throw new StripeSignatureException("Webhook Stripe não configurado.");
        if (string.IsNullOrWhiteSpace(signatureHeader))
            throw new StripeSignatureException("Assinatura do webhook em falta.");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret, 300, throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            throw new StripeSignatureException(ex.Message);
        }
        catch (Exception ex) when (ex is not StripeSignatureException)
        {
            throw new StripeSignatureException("Assinatura do webhook inválida.");
        }

        return MapEvent(stripeEvent);
    }

    internal static StripeWebhookNotification MapEvent(Event stripeEvent)
    {
        var metadata = MetadataOf(stripeEvent.Data.Object);
        return stripeEvent.Type switch
        {
            "checkout.session.completed" when stripeEvent.Data.Object is Session completed =>
                new CheckoutSessionCompletedNotification(
                    stripeEvent.Id,
                    completed.Id,
                    completed.PaymentIntentId,
                    string.Equals(completed.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase),
                    metadata),
            "checkout.session.async_payment_succeeded" when stripeEvent.Data.Object is Session asyncPaid =>
                new AsyncPaymentSucceededNotification(stripeEvent.Id, asyncPaid.Id, asyncPaid.PaymentIntentId, metadata),
            "checkout.session.async_payment_failed" when stripeEvent.Data.Object is Session asyncFailed =>
                new AsyncPaymentFailedNotification(stripeEvent.Id, asyncFailed.Id, asyncFailed.PaymentIntentId, metadata),
            "payment_intent.payment_failed" when stripeEvent.Data.Object is PaymentIntent intent =>
                new PaymentIntentFailedNotification(stripeEvent.Id, intent.Id, intent.LastPaymentError?.Message, metadata),
            "charge.refunded" when stripeEvent.Data.Object is Charge charge =>
                new ChargeRefundedNotification(stripeEvent.Id, charge.PaymentIntentId, charge.Id, charge.AmountRefunded, charge.Refunded),
            "account.updated" when stripeEvent.Data.Object is Account account =>
                MapAccountNotification(stripeEvent.Id, account),
            _ => new IgnoredStripeNotification(stripeEvent.Id, stripeEvent.Type)
        };
    }

    private static AccountUpdatedNotification MapAccountNotification(string eventId, Account account)
    {
        var snapshot = MapAccount(account);
        return new AccountUpdatedNotification(
            eventId,
            snapshot.AccountId,
            snapshot.ChargesEnabled,
            snapshot.PayoutsEnabled,
            snapshot.DetailsSubmitted,
            snapshot.CardPayments,
            snapshot.MbWay,
            snapshot.Multibanco);
    }

    private static ConnectedAccountSnapshot MapAccount(Account account)
    {
        var caps = account.Capabilities;
        return new ConnectedAccountSnapshot(
            account.Id,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted,
            Active(caps?.CardPayments),
            Active(caps?.MbWayPayments),
            Active(caps?.MultibancoPayments));
    }

    private static bool Active(string? capability) =>
        string.Equals(capability, "active", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, string> MetadataOf(object? entity) =>
        entity switch
        {
            Session session => session.Metadata ?? new Dictionary<string, string>(),
            PaymentIntent intent => intent.Metadata ?? new Dictionary<string, string>(),
            _ => new Dictionary<string, string>()
        };

    private static async Task<T> CallAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (StripeException ex)
        {
            var message = ex.StripeError?.Message ?? ex.Message;
            throw new InvalidOperationException("O Stripe recusou o pedido: " + message);
        }
    }
}
