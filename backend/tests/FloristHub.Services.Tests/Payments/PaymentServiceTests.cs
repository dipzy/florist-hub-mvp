using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services;
using FloristHub.Services.Email;
using FloristHub.DAL.Legal;
using FloristHub.Services.Payments;
using FloristHub.Services.Tests.Email;
using FloristHub.Services.Visualization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Tests.Payments;

public class PaymentServiceTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Checkout_is_disabled_without_stripe_and_creates_nothing()
    {
        var fx = Create(configured: false);
        var order = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 20m);

        var result = await fx.Payments.StartCheckoutAsync(order.Id, order.CustomerId, "https://loja.test/ok", "https://loja.test/cancel");

        Assert.False(result.PaymentsEnabled);
        Assert.Null(result.CheckoutUrl);
        Assert.Empty(await fx.Db.Payments.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Checkout_requires_confirmed_order_and_uses_tenant_commission()
    {
        var fx = Create();
        var pending = await SeedOrderAsync(fx, TenantA, OrderStatus.PendingConfirmation, 25.50m);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Payments.StartCheckoutAsync(pending.Id, pending.CustomerId, "https://loja.test/ok", "https://loja.test/cancel"));

        var order = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 25.50m);
        fx.Db.Tenants.Single(t => t.Id == TenantA).CommissionPercentOverride = 12.5m;
        await fx.Db.SaveChangesAsync();

        var result = await fx.Payments.StartCheckoutAsync(order.Id, order.CustomerId, "https://loja.test/ok", "https://loja.test/cancel");

        Assert.True(result.PaymentsEnabled);
        Assert.Equal("https://checkout.stripe.test/cs_test", result.CheckoutUrl);
        var payment = await fx.Db.Payments.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(12.5m, payment.CommissionPercent);
        Assert.Equal(319, payment.ApplicationFeeAmountCents);
        Assert.Equal(319, fx.Stripe.Checkouts.Single().ApplicationFeeCents);
        Assert.True(fx.Stripe.Checkouts.Single().MbWay);
        Assert.True(fx.Stripe.Checkouts.Single().Multibanco);
        Assert.Contains(payment.Events, e => e.EventType == "checkout.created");
    }

    [Fact]
    public async Task Checkout_uses_global_commission_when_tenant_has_no_override()
    {
        var fx = Create();
        await fx.Payments.UpdatePlatformBillingAsync(8m, 19.90m, Guid.NewGuid());
        var order = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 10m);

        await fx.Payments.StartCheckoutAsync(order.Id, order.CustomerId, "https://loja.test/ok", "https://loja.test/cancel");

        Assert.Equal(80, fx.Stripe.Checkouts.Single().ApplicationFeeCents);
        var billing = await fx.Payments.GetPlatformBillingAsync();
        Assert.Equal(19.90m, billing.MonthlyFeeAmount);
    }

    [Fact]
    public async Task Checkout_does_not_see_another_tenants_order()
    {
        var fx = Create();
        var order = await SeedOrderAsync(fx, TenantB, OrderStatus.Confirmed, 10m);

        await         Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fx.Payments.StartCheckoutAsync(order.Id, order.CustomerId, "https://loja.test/ok", "https://loja.test/cancel"));
    }

    [Fact]
    public async Task Checkout_stores_a_valid_customer_nif_and_rejects_an_invalid_one()
    {
        var fx = Create();
        var order = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 10m);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Payments.StartCheckoutAsync(order.Id, order.CustomerId, "https://loja.test/ok", "https://loja.test/cancel", "123456780"));

        await fx.Payments.StartCheckoutAsync(order.Id, order.CustomerId, "https://loja.test/ok", "https://loja.test/cancel", "PT123456789");
        Assert.Equal("123456789", (await fx.Db.Orders.IgnoreQueryFilters().SingleAsync()).CustomerTaxId);
    }

    [Fact]
    public async Task Webhook_checkout_completed_is_idempotent_and_sends_email()
    {
        var fx = Create();
        var order = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 30m);
        var payment = await SeedPaymentAsync(fx, order, PaymentStatus.Pending, "cs_1", "pi_1");
        fx.Stripe.Notification = new CheckoutSessionCompletedNotification(
            "evt_1", "cs_1", "pi_1", true, new Dictionary<string, string> { ["paymentId"] = payment.Id.ToString() });

        await fx.Payments.HandleWebhookAsync("{}", "sig");
        await fx.Payments.HandleWebhookAsync("{}", "sig");

        var stored = await fx.Db.Payments.IgnoreQueryFilters().Include(p => p.Events).SingleAsync();
        var savedOrder = await fx.Db.Orders.IgnoreQueryFilters().SingleAsync(o => o.Id == order.Id);
        Assert.Equal(PaymentStatus.Succeeded, stored.Status);
        Assert.True(savedOrder.PaymentCaptured);
        Assert.Equal(1, await fx.Db.StripeWebhookReceipts.CountAsync());
        Assert.Equal(2, fx.Emails.Messages.Count);
        Assert.Contains(fx.Emails.Messages, m => m.To == "cliente@loja.pt");
        Assert.Contains(fx.Emails.Messages, m => m.To == "florista@loja.pt");
        Assert.Single(stored.Events, e => e.EventType == "checkout.session.completed");
        Assert.Equal([payment.Id], fx.Invoices.Receipts);
    }

    [Fact]
    public async Task Webhook_rejects_invalid_signature_without_changing_state()
    {
        var fx = Create();
        var order = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 30m);
        await SeedPaymentAsync(fx, order, PaymentStatus.Pending, "cs_1", "pi_1");
        fx.Stripe.FailSignature = true;

        await Assert.ThrowsAsync<StripeSignatureException>(() => fx.Payments.HandleWebhookAsync("{}", "invalid"));

        Assert.Equal(PaymentStatus.Pending, (await fx.Db.Payments.IgnoreQueryFilters().SingleAsync()).Status);
        Assert.Empty(fx.Db.StripeWebhookReceipts);
        Assert.Empty(fx.Emails.Messages);
    }

    [Fact]
    public async Task Webhook_payment_failed_and_does_not_downgrade_a_paid_order()
    {
        var fx = Create();
        var order = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 30m);
        var payment = await SeedPaymentAsync(fx, order, PaymentStatus.Pending, "cs_1", "pi_1");
        fx.Stripe.Notification = new PaymentIntentFailedNotification(
            "evt_fail", "pi_1", "Cartão recusado", new Dictionary<string, string> { ["paymentId"] = payment.Id.ToString() });

        await fx.Payments.HandleWebhookAsync("{}", "sig");
        Assert.Equal(PaymentStatus.Failed, (await fx.Db.Payments.IgnoreQueryFilters().SingleAsync()).Status);

        payment.Status = PaymentStatus.Succeeded;
        await fx.Db.SaveChangesAsync();
        fx.Stripe.Notification = new PaymentIntentFailedNotification(
            "evt_fail_2", "pi_1", "Tarde demais", new Dictionary<string, string>());
        await fx.Payments.HandleWebhookAsync("{}", "sig");

        Assert.Equal(PaymentStatus.Succeeded, (await fx.Db.Payments.IgnoreQueryFilters().SingleAsync()).Status);
    }

    [Fact]
    public async Task Webhook_charge_refunded_sets_partial_and_full_amounts()
    {
        var fx = Create();
        var order = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 40m);
        order.PaymentCaptured = true;
        await fx.Db.SaveChangesAsync();
        await SeedPaymentAsync(fx, order, PaymentStatus.Succeeded, "cs_1", "pi_1");

        fx.Stripe.Notification = new ChargeRefundedNotification("evt_r1", "pi_1", "ch_1", 1500, false);
        await fx.Payments.HandleWebhookAsync("{}", "sig");
        var partial = await fx.Db.Payments.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, partial.Status);
        Assert.Equal(15m, partial.RefundedAmount);
        Assert.True((await fx.Db.Orders.IgnoreQueryFilters().SingleAsync()).PaymentCaptured);

        fx.Stripe.Notification = new ChargeRefundedNotification("evt_r2", "pi_1", "ch_1", 4000, true);
        await fx.Payments.HandleWebhookAsync("{}", "sig");
        var full = await fx.Db.Payments.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(PaymentStatus.Refunded, full.Status);
        Assert.Equal(40m, full.RefundedAmount);
        Assert.False((await fx.Db.Orders.IgnoreQueryFilters().SingleAsync()).PaymentCaptured);
        Assert.Equal(2, fx.Invoices.Credits.Count);
    }

    [Fact]
    public async Task Webhook_account_updated_is_isolated_by_stripe_account()
    {
        var fx = Create();
        fx.Stripe.Notification = new AccountUpdatedNotification("evt_acc", "acct_a", true, true, true, true, true, false);
        await fx.Payments.HandleWebhookAsync("{}", "sig");

        var a = await fx.Db.Tenants.SingleAsync(t => t.Id == TenantA);
        var b = await fx.Db.Tenants.SingleAsync(t => t.Id == TenantB);
        Assert.True(a.StripeChargesEnabled);
        Assert.True(a.StripeMbWay);
        Assert.False(a.StripeMultibanco);
        Assert.False(b.StripeChargesEnabled);
    }

    [Fact]
    public async Task Florist_lists_only_the_current_tenant_payments()
    {
        var fx = Create();
        var orderA = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 10m);
        var orderB = await SeedOrderAsync(fx, TenantB, OrderStatus.Confirmed, 99m);
        await SeedPaymentAsync(fx, orderA, PaymentStatus.Succeeded, "cs_a", "pi_a");
        await SeedPaymentAsync(fx, orderB, PaymentStatus.Succeeded, "cs_b", "pi_b");

        var list = await fx.Payments.ListForFloristAsync();

        Assert.Single(list);
        Assert.Equal(orderA.Id, list[0].OrderId);
        var otherId = (await fx.Db.Payments.IgnoreQueryFilters().SingleAsync(p => p.OrderId == orderB.Id)).Id;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => fx.Payments.RefundAsync(otherId, 1m, "não", Guid.NewGuid()));
    }

    [Fact]
    public async Task Refund_rules_reject_excess_and_allow_partial_then_rest()
    {
        var fx = Create();
        var order = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 25.50m);
        var payment = await SeedPaymentAsync(fx, order, PaymentStatus.Succeeded, "cs_1", "pi_1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => fx.Payments.RefundAsync(payment.Id, 0m, null, Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fx.Payments.RefundAsync(payment.Id, 26m, null, Guid.NewGuid()));

        var partial = await fx.Payments.RefundAsync(payment.Id, 10m, "Parte", Guid.NewGuid());
        Assert.Equal(PaymentStatus.PartiallyRefunded, partial.Status);
        Assert.Equal(10m, partial.RefundedAmount);
        Assert.Equal(1000, fx.Stripe.Refunds[0].AmountCents);

        var rest = await fx.Payments.RefundAsync(payment.Id, null, "Resto", Guid.NewGuid());
        Assert.Equal(PaymentStatus.Refunded, rest.Status);
        Assert.Equal(25.50m, rest.RefundedAmount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fx.Payments.RefundAsync(payment.Id, 1m, null, Guid.NewGuid()));
    }

    [Fact]
    public async Task Customer_cancel_of_paid_confirmed_order_refunds_in_full()
    {
        var fx = Create();
        var order = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 18m);
        await SeedPaymentAsync(fx, order, PaymentStatus.Succeeded, "cs_1", "pi_1");
        var orders = new OrderService(fx.Uow, fx.TenantContext, fx.Audit, fx.Payments, new NoOpArrangement3DService(), fx.EmailService, new NoOrderPushNotifications(), fx.Legal, new FloristHub.Services.Tests.Notifications.NoFloristPanelNotificationService());

        var dto = await orders.CancelAsync(order.Id, order.CustomerId, new FloristHub.Services.DTOs.Orders.CancelOrderRequest("Desisti"));

        Assert.Equal(OrderStatus.Cancelled, dto.Status);
        Assert.Null(Assert.Single(fx.Stripe.Refunds).AmountCents);
        var payment = await fx.Db.Payments.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(18m, payment.RefundedAmount);
    }

    [Fact]
    public async Task Customer_cannot_cancel_after_preparation_and_unpaid_cancel_drops_checkout()
    {
        var fx = Create();
        var preparing = await SeedOrderAsync(fx, TenantA, OrderStatus.InPreparation, 18m);
        await SeedPaymentAsync(fx, preparing, PaymentStatus.Succeeded, "cs_p", "pi_p");
        var orders = new OrderService(fx.Uow, fx.TenantContext, fx.Audit, fx.Payments, new NoOpArrangement3DService(), fx.EmailService, new NoOrderPushNotifications(), fx.Legal, new FloristHub.Services.Tests.Notifications.NoFloristPanelNotificationService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orders.CancelAsync(preparing.Id, preparing.CustomerId, new FloristHub.Services.DTOs.Orders.CancelOrderRequest(null)));
        Assert.Empty(fx.Stripe.Refunds);

        var confirmed = await SeedOrderAsync(fx, TenantA, OrderStatus.Confirmed, 18m);
        var pendingPayment = await SeedPaymentAsync(fx, confirmed, PaymentStatus.Pending, "cs_pending", null);
        await orders.CancelAsync(confirmed.Id, confirmed.CustomerId, new FloristHub.Services.DTOs.Orders.CancelOrderRequest("Ainda não paguei"));

        Assert.Equal(PaymentStatus.Cancelled, (await fx.Db.Payments.IgnoreQueryFilters().SingleAsync(p => p.Id == pendingPayment.Id)).Status);
        Assert.Empty(fx.Stripe.Refunds);
    }

    [Fact]
    public void Monthly_fee_is_stored_and_not_part_of_the_checkout_fee()
    {
        CommissionCalculator.EnsureMonthlyFee(0);
        CommissionCalculator.EnsureMonthlyFee(29.90m);
        Assert.Throws<InvalidOperationException>(() => CommissionCalculator.EnsureMonthlyFee(-1));
        Assert.Equal(0, CommissionCalculator.ApplicationFeeCents(40m, 0));
    }

    private static async Task<Order> SeedOrderAsync(Fixture fx, Guid tenantId, OrderStatus status, decimal total)
    {
        var customerId = Guid.NewGuid();
        var customer = new ApplicationUser
        {
            Id = customerId,
            Email = "cliente@loja.pt",
            UserName = $"cliente-{customerId:N}@loja.pt",
            FullName = "Ana Costa",
            TenantId = tenantId
        };
        fx.Db.Users.Add(customer);
        var order = new Order
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Customer = customer,
            Status = status,
            EstimatedTotal = total,
            DeliveryType = DeliveryType.StorePickup,
            BaseType = ArrangementBaseType.Sponge
        };
        fx.Db.Orders.Add(order);
        await fx.Db.SaveChangesAsync();
        return order;
    }

    private static async Task<Payment> SeedPaymentAsync(Fixture fx, Order order, PaymentStatus status, string sessionId, string? paymentIntentId)
    {
        var payment = new Payment
        {
            TenantId = order.TenantId,
            OrderId = order.Id,
            Status = status,
            Amount = order.EstimatedTotal,
            Currency = "eur",
            CommissionPercent = 10m,
            ApplicationFeeAmountCents = 100,
            StripeCheckoutSessionId = sessionId,
            StripePaymentIntentId = paymentIntentId,
            StripeConnectedAccountId = order.TenantId == TenantA ? "acct_a" : "acct_b",
            CheckoutUrl = "https://checkout.stripe.test/" + sessionId
        };
        fx.Db.Payments.Add(payment);
        await fx.Db.SaveChangesAsync();
        return payment;
    }

    private static Fixture Create(bool configured = true)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(TenantA, "loja-a");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options, tenantContext);
        db.Tenants.AddRange(
            new Tenant { Id = TenantA, Name = "Loja A", Slug = "loja-a", ContactEmail = "florista@loja.pt", StripeAccountId = "acct_a", StripeChargesEnabled = true },
            new Tenant { Id = TenantB, Name = "Loja B", Slug = "loja-b", ContactEmail = "b@loja.pt", StripeAccountId = "acct_b" });
        db.LegalDocumentVersions.AddRange(
            new LegalDocumentVersion { DocumentType = LegalDocumentType.Terms, Version = LegalDocumentSeedContent.SeedVersion, Title = "Termos", ContentMarkdown = "t", IsCurrent = true },
            new LegalDocumentVersion { DocumentType = LegalDocumentType.Privacy, Version = LegalDocumentSeedContent.SeedVersion, Title = "Privacidade", ContentMarkdown = "p", IsCurrent = true });
        db.SaveChanges();

        var stripe = new FakeStripeGateway { IsConfigured = configured };
        var emails = new FakeEmailQueue();
        var emailService = new OrderEmailNotificationsService(emails, Options.Create(new EmailOptions()));
        var uow = new UnitOfWork(db);
        var audit = new AuditLogger(uow, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditLogger>.Instance);
        var invoices = new RecordingInvoices();
        var payments = new PaymentService(
            uow,
            tenantContext,
            stripe,
            emailService,
            new NoOrderPushNotifications(),
            audit,
            new FloristHub.Services.Tests.Notifications.NoFloristPanelNotificationService(),
            Options.Create(new EmailOptions { WebBaseUrl = "https://loja.test" }),
            Options.Create(new StripeOptions { PublishableKey = "pk_test_example" }),
            invoices);
        var legal = new LegalComplianceService(uow, tenantContext, audit);
        return new Fixture(db, uow, tenantContext, stripe, emails, emailService, audit, payments, invoices, legal);
    }

    private sealed record Fixture(
        ApplicationDbContext Db,
        UnitOfWork Uow,
        TenantContext TenantContext,
        FakeStripeGateway Stripe,
        FakeEmailQueue Emails,
        OrderEmailNotificationsService EmailService,
        IAuditLogger Audit,
        PaymentService Payments,
        RecordingInvoices Invoices,
        LegalComplianceService Legal);

    private sealed class RecordingInvoices : FloristHub.Services.Invoicing.IInvoiceIssuance
    {
        public List<Guid> Receipts { get; } = [];
        public List<Guid> Credits { get; } = [];

        public Task EnsureInvoiceReceiptAsync(Guid paymentId, CancellationToken ct = default)
        {
            Receipts.Add(paymentId);
            return Task.CompletedTask;
        }

        public Task EnsureCreditNoteAsync(Guid paymentId, CancellationToken ct = default)
        {
            Credits.Add(paymentId);
            return Task.CompletedTask;
        }
    }
}

internal sealed class NoOrderPushNotifications : FloristHub.Services.Interfaces.Push.IOrderPushNotifications
{
    public void OrderCreated(Order order, Tenant tenant) { }
    public void OrderPaid(Order order, Tenant tenant) { }
    public void OrderStatusChanged(Order order, Tenant tenant, OrderStatus status) { }
}
