using Microsoft.Extensions.Options;
using Stripe;
using FloristHub.Services.Payments;

namespace FloristHub.Services.Tests.Payments;

public class StripeWebhookSignatureTests
{
    private const string Secret = "whsec_test_secret";

    [Fact]
    public void Valid_signature_parses_checkout_session_completed()
    {
        var paymentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var payload = $$"""
            {
              "id": "evt_signature",
              "object": "event",
              "api_version": "2024-06-20",
              "created": 1710000000,
              "type": "checkout.session.completed",
              "data": {
                "object": {
                  "id": "cs_live_example",
                  "object": "checkout.session",
                  "payment_status": "paid",
                  "payment_intent": "pi_example",
                  "metadata": { "paymentId": "{{paymentId}}" }
                }
              }
            }
            """;
        var gateway = Gateway();
        var header = EventUtility.GenerateSignatureHeader(payload, Secret);

        var notification = gateway.ParseWebhook(payload, header);

        var completed = Assert.IsType<CheckoutSessionCompletedNotification>(notification);
        Assert.Equal("evt_signature", completed.EventId);
        Assert.Equal("cs_live_example", completed.SessionId);
        Assert.Equal("pi_example", completed.PaymentIntentId);
        Assert.True(completed.Paid);
        Assert.Equal(paymentId.ToString(), completed.Metadata["paymentId"]);
    }

    [Fact]
    public void Tampered_payload_is_rejected()
    {
        const string payload = """{"id":"evt_x","object":"event","type":"account.updated","data":{"object":{"id":"acct_x","object":"account"}}}""";
        var header = EventUtility.GenerateSignatureHeader(payload, Secret);
        var gateway = Gateway();

        Assert.Throws<StripeSignatureException>(() => gateway.ParseWebhook(payload + " ", header));
        Assert.Throws<StripeSignatureException>(() => gateway.ParseWebhook(payload, "t=1,v1=deadbeef"));
    }

    [Fact]
    public void Missing_webhook_secret_rejects_every_call()
    {
        var gateway = new StripeGateway(Options.Create(new StripeOptions { SecretKey = "sk_test_example", WebhookSecret = "" }));
        Assert.Throws<StripeSignatureException>(() => gateway.ParseWebhook("{}", "t=1,v1=abc"));
    }

    private static StripeGateway Gateway() =>
        new(Options.Create(new StripeOptions { SecretKey = "sk_test_example", WebhookSecret = Secret }));
}
