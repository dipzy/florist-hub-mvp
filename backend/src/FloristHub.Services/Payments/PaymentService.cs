using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Payments;
using FloristHub.Services.Email;
using FloristHub.Services.Invoicing;
using FloristHub.Services.Interfaces.Email;
using FloristHub.Services.Interfaces.Push;
using FloristHub.Services.Notifications;
using FloristHub.Services.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Payments;

public interface IPaymentService
{
    Task<CheckoutSessionDto> StartCheckoutAsync(Guid orderId, Guid customerId, string? successUrl, string? cancelUrl, string? customerTaxId = null, CancellationToken ct = default);
    Task HandleWebhookAsync(string payload, string signatureHeader, CancellationToken ct = default);
    Task<StripeConnectStatusDto> GetConnectStatusAsync(CancellationToken ct = default);
    Task<StripeOnboardingResponse> StartOnboardingAsync(string returnUrl, string refreshUrl, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentDto>> ListForFloristAsync(CancellationToken ct = default);
    Task<PaymentDto> RefundAsync(Guid paymentId, decimal? amount, string? reason, Guid floristUserId, CancellationToken ct = default);
    Task SettleCustomerCancellationAsync(Guid orderId, CancellationToken ct = default);
    Task<PlatformBillingDto> GetPlatformBillingAsync(CancellationToken ct = default);
    Task<PlatformBillingDto> UpdatePlatformBillingAsync(decimal commissionPercent, decimal monthlyFeeAmount, Guid actorId, CancellationToken ct = default);
    Task<TenantBillingDto> GetTenantBillingAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantBillingDto> UpdateTenantBillingAsync(Guid tenantId, decimal? commissionPercentOverride, decimal? monthlyFeeAmount, Guid actorId, CancellationToken ct = default);
}

public sealed class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IStripeGateway _stripe;
    private readonly IOrderEmailNotifications _emails;
    private readonly IOrderPushNotifications _push;
    private readonly IAuditLogger _audit;
    private readonly EmailOptions _emailOptions;
    private readonly StripeOptions _stripeOptions;
    private readonly IInvoiceIssuance _invoices;
    private readonly IStockService? _stock;
    private readonly IFloristPanelNotificationService _panel;

    public PaymentService(
        IUnitOfWork unitOfWork,
        ITenantContext tenant,
        IStripeGateway stripe,
        IOrderEmailNotifications emails,
        IOrderPushNotifications push,
        IAuditLogger audit,
        IFloristPanelNotificationService panel,
        IOptions<EmailOptions> emailOptions,
        IOptions<StripeOptions> stripeOptions,
        IInvoiceIssuance invoices,
        IStockService? stock = null)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _stripe = stripe;
        _emails = emails;
        _push = push;
        _audit = audit;
        _panel = panel;
        _emailOptions = emailOptions.Value;
        _stripeOptions = stripeOptions.Value;
        _invoices = invoices;
        _stock = stock;
    }

    public async Task<CheckoutSessionDto> StartCheckoutAsync(
        Guid orderId,
        Guid customerId,
        string? successUrl,
        string? cancelUrl,
        string? customerTaxId = null,
        CancellationToken ct = default)
    {
        if (!_stripe.IsConfigured)
            return new CheckoutSessionDto(false, null, null, null, "Os pagamentos estão desativados.");

        var order = await _unitOfWork.Orders.Query()
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new KeyNotFoundException("Encomenda não encontrada.");

        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("Encomenda não pertence ao cliente.");
        if (order.Status is not (OrderStatus.Confirmed or OrderStatus.InPreparation or OrderStatus.Ready))
            throw new InvalidOperationException("O pagamento só fica disponível depois de a florista confirmar a encomenda.");

        var latest = await _unitOfWork.Payments.GetLatestForOrderAsync(order.Id, ct);
        var settled = latest is { Status: PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded };
        if (customerTaxId is not null && !settled)
        {
            order.CustomerTaxId = string.IsNullOrWhiteSpace(customerTaxId) ? null : PortugueseNif.Normalize(customerTaxId);
            order.UpdatedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }
        if (settled)
            return new CheckoutSessionDto(true, null, latest!.Id, latest.Status, "Esta encomenda já está paga.");
        if (latest is { Status: PaymentStatus.Pending, CheckoutUrl: not null })
            return new CheckoutSessionDto(true, latest.CheckoutUrl, latest.Id, latest.Status, null);

        var tenant = await RequireTenantEntityAsync(order.TenantId, ct);
        string accountId;
        ConnectedAccountSnapshot snapshot;
        if (_stripe.SimulatesCheckout)
        {
            accountId = string.IsNullOrWhiteSpace(tenant.StripeAccountId) ? "acct_dev_fake" : tenant.StripeAccountId;
            snapshot = await _stripe.GetAccountAsync(accountId, ct);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(tenant.StripeAccountId) || !tenant.StripeChargesEnabled)
                throw new InvalidOperationException("A florista ainda não tem a conta Stripe pronta a receber pagamentos.");

            snapshot = await _stripe.GetAccountAsync(tenant.StripeAccountId, ct);
            ApplySnapshot(tenant, snapshot);
            if (!snapshot.ChargesEnabled)
                throw new InvalidOperationException("A florista ainda não tem a conta Stripe pronta a receber pagamentos.");
            accountId = tenant.StripeAccountId;
        }

        var settings = await GetOrCreateSettingsAsync(ct);
        var percent = CommissionCalculator.ResolvePercent(settings.CommissionPercent, tenant.CommissionPercentOverride);
        var feeCents = CommissionCalculator.ApplicationFeeCents(order.EstimatedTotal, percent);
        var success = RequireUrl(successUrl, $"/encomendas/{order.Id}/pagamento/sucesso");
        var cancel = RequireUrl(cancelUrl, $"/encomendas/{order.Id}/pagamento/cancelado");

        var payment = new Payment
        {
            TenantId = order.TenantId,
            OrderId = order.Id,
            Status = PaymentStatus.Pending,
            Amount = order.EstimatedTotal,
            Currency = "eur",
            CommissionPercent = percent,
            ApplicationFeeAmountCents = feeCents,
            StripeConnectedAccountId = accountId
        };
        await _unitOfWork.Payments.AddAsync(payment, ct);
        await AddHistoryAsync(payment, null, PaymentStatus.Pending, "checkout.created", null, "Checkout criado.", null, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var session = await _stripe.CreateCheckoutSessionAsync(new CreateCheckoutCommand(
            accountId,
            CommissionCalculator.ToCents(order.EstimatedTotal),
            feeCents > 0 ? feeCents : null,
            success,
            cancel,
            $"Encomenda {order.Id.ToString()[..8]}",
            order.Customer?.Email,
            order.Id,
            payment.Id,
            order.TenantId,
            snapshot.CardPayments || (!snapshot.MbWay && !snapshot.Multibanco),
            snapshot.MbWay,
            snapshot.Multibanco), ct);

        payment.StripeCheckoutSessionId = session.SessionId;
        payment.CheckoutUrl = session.Url;
        payment.StripePaymentIntentId = session.PaymentIntentId;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        order.PaymentReference = session.SessionId;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("CheckoutStarted", customerId, order.TenantId, nameof(Payment), payment.Id, session.SessionId, ct);
        if (session.SettleImmediately)
        {
            await ApplyCheckoutPaidAsync(
                "evt_fake_" + payment.Id.ToString("N"),
                session.SessionId,
                session.PaymentIntentId,
                true,
                new Dictionary<string, string> { ["paymentId"] = payment.Id.ToString() },
                "checkout.session.completed",
                ct);
        }

        return new CheckoutSessionDto(true, session.Url, payment.Id, payment.Status, null);
    }

    public async Task HandleWebhookAsync(string payload, string signatureHeader, CancellationToken ct = default)
    {
        if (!_stripe.IsConfigured)
            throw new InvalidOperationException("Pagamentos Stripe desativados.");

        var notification = _stripe.ParseWebhook(payload, signatureHeader);
        if (await _unitOfWork.WebhookReceipts.Query().AnyAsync(r => r.StripeEventId == notification.EventId, ct))
            return;

        switch (notification)
        {
            case CheckoutSessionCompletedNotification completed:
                await ApplyCheckoutPaidAsync(completed.EventId, completed.SessionId, completed.PaymentIntentId, completed.Paid, completed.Metadata, completed.Type, ct);
                break;
            case AsyncPaymentSucceededNotification asyncPaid:
                await ApplyCheckoutPaidAsync(asyncPaid.EventId, asyncPaid.SessionId, asyncPaid.PaymentIntentId, true, asyncPaid.Metadata, asyncPaid.Type, ct);
                break;
            case AsyncPaymentFailedNotification asyncFailed:
                await ApplyFailureAsync(asyncFailed.EventId, asyncFailed.Metadata, asyncFailed.SessionId, asyncFailed.PaymentIntentId, "O pagamento assíncrono falhou.", asyncFailed.Type, ct);
                break;
            case PaymentIntentFailedNotification failed:
                await ApplyFailureAsync(failed.EventId, failed.Metadata, null, failed.PaymentIntentId, failed.Message ?? "O pagamento falhou.", failed.Type, ct);
                break;
            case ChargeRefundedNotification refunded:
                await ApplyChargeRefundedAsync(refunded, ct);
                break;
            case AccountUpdatedNotification account:
                await ApplyAccountUpdatedAsync(account, ct);
                break;
        }

        await _unitOfWork.WebhookReceipts.AddAsync(new StripeWebhookReceipt
        {
            StripeEventId = notification.EventId,
            EventType = notification.Type
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        if (_stock is not null)
            await _stock.FlushNoticesAsync(ct);
    }

    public async Task<StripeConnectStatusDto> GetConnectStatusAsync(CancellationToken ct = default)
    {
        if (_stripe.SimulatesCheckout)
            return new StripeConnectStatusDto(true, true, true, true, true, true, true, true, "acct_dev_fake", null);

        var tenant = await RequireCurrentTenantAsync(ct);
        if (_stripe.IsConfigured && !string.IsNullOrWhiteSpace(tenant.StripeAccountId))
        {
            var snapshot = await _stripe.GetAccountAsync(tenant.StripeAccountId, ct);
            ApplySnapshot(tenant, snapshot);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return MapConnect(tenant);
    }

    public async Task<StripeOnboardingResponse> StartOnboardingAsync(string returnUrl, string refreshUrl, CancellationToken ct = default)
    {
        if (!_stripe.IsConfigured)
            return new StripeOnboardingResponse(false, null, "Os pagamentos estão desativados.");

        EnsureAbsoluteUrl(returnUrl);
        EnsureAbsoluteUrl(refreshUrl);
        var tenant = await RequireCurrentTenantAsync(ct);
        if (string.IsNullOrWhiteSpace(tenant.StripeAccountId))
        {
            var created = await _stripe.CreateExpressAccountAsync(
                new CreateExpressAccountCommand(tenant.ContactEmail, tenant.Id, tenant.Name), ct);
            tenant.StripeAccountId = created.AccountId;
            tenant.UpdatedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            await _audit.LogAsync("StripeAccountCreated", null, tenant.Id, nameof(Tenant), tenant.Id, created.AccountId, ct);
        }

        var url = await _stripe.CreateAccountLinkAsync(
            new AccountLinkCommand(tenant.StripeAccountId!, refreshUrl, returnUrl), ct);
        return new StripeOnboardingResponse(true, url, null);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListForFloristAsync(CancellationToken ct = default)
    {
        _ = RequireTenantId();
        var payments = await _unitOfWork.Payments.ListForCurrentTenantAsync(ct);
        return payments.Select(Map).ToList();
    }

    public async Task<PaymentDto> RefundAsync(Guid paymentId, decimal? amount, string? reason, Guid floristUserId, CancellationToken ct = default)
    {
        if (!_stripe.IsConfigured)
            throw new InvalidOperationException("Pagamentos Stripe desativados.");

        var payment = await _unitOfWork.Payments.GetWithEventsAsync(paymentId, ct)
            ?? throw new KeyNotFoundException("Pagamento não encontrado.");
        var refundAmount = PaymentRefundRules.ResolveRefundAmount(payment, amount);
        await ExecuteRefundAsync(payment, refundAmount, reason ?? "Reembolso pela florista.", floristUserId, ct);
        return Map(await _unitOfWork.Payments.GetWithEventsAsync(payment.Id, ct) ?? payment);
    }

    public async Task SettleCustomerCancellationAsync(Guid orderId, CancellationToken ct = default)
    {
        var payment = await _unitOfWork.Payments.GetLatestForOrderAsync(orderId, ct);
        if (payment is null)
            return;

        if (PaymentRefundRules.CustomerCancellationRequiresFullRefund(payment))
        {
            if (!_stripe.IsConfigured)
                throw new InvalidOperationException("Não é possível cancelar uma encomenda paga enquanto os pagamentos Stripe estão desativados.");
            await ExecuteRefundAsync(payment, PaymentRefundRules.Remaining(payment), "Cancelamento pelo cliente.", null, ct);
            return;
        }

        if (payment.Status == PaymentStatus.Pending)
        {
            var from = payment.Status;
            payment.Status = PaymentStatus.Cancelled;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            await AddHistoryAsync(payment, from, PaymentStatus.Cancelled, "payment.cancelled", null, "Checkout cancelado com a encomenda.", null, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<PlatformBillingDto> GetPlatformBillingAsync(CancellationToken ct = default)
    {
        var settings = await GetOrCreateSettingsAsync(ct);
        return new PlatformBillingDto(settings.CommissionPercent, settings.MonthlyFeeAmount);
    }

    public async Task<PlatformBillingDto> UpdatePlatformBillingAsync(
        decimal commissionPercent,
        decimal monthlyFeeAmount,
        Guid actorId,
        CancellationToken ct = default)
    {
        CommissionCalculator.EnsurePercent(commissionPercent);
        CommissionCalculator.EnsureMonthlyFee(monthlyFeeAmount);
        var settings = await GetOrCreateSettingsAsync(ct);
        settings.CommissionPercent = commissionPercent;
        settings.MonthlyFeeAmount = monthlyFeeAmount;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("PlatformBillingUpdated", actorId, null, nameof(PlatformBillingSettings), settings.Id, $"{commissionPercent}% / {monthlyFeeAmount}", ct);
        return new PlatformBillingDto(settings.CommissionPercent, settings.MonthlyFeeAmount);
    }

    public async Task<TenantBillingDto> GetTenantBillingAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await RequireTenantEntityAsync(tenantId, ct);
        var settings = await GetOrCreateSettingsAsync(ct);
        return MapTenantBilling(tenant, settings);
    }

    public async Task<TenantBillingDto> UpdateTenantBillingAsync(
        Guid tenantId,
        decimal? commissionPercentOverride,
        decimal? monthlyFeeAmount,
        Guid actorId,
        CancellationToken ct = default)
    {
        if (commissionPercentOverride is decimal percent)
            CommissionCalculator.EnsurePercent(percent);
        if (monthlyFeeAmount is decimal fee)
            CommissionCalculator.EnsureMonthlyFee(fee);

        var tenant = await _unitOfWork.Tenants.Query().FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");
        tenant.CommissionPercentOverride = commissionPercentOverride;
        tenant.MonthlyFeeAmount = monthlyFeeAmount;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("TenantBillingUpdated", actorId, tenant.Id, nameof(Tenant), tenant.Id, $"{commissionPercentOverride}/{monthlyFeeAmount}", ct);
        var settings = await GetOrCreateSettingsAsync(ct);
        return MapTenantBilling(tenant, settings);
    }

    private async Task ApplyCheckoutPaidAsync(
        string eventId,
        string sessionId,
        string? paymentIntentId,
        bool paid,
        IReadOnlyDictionary<string, string> metadata,
        string eventType,
        CancellationToken ct)
    {
        var payment = await FindPaymentAsync(metadata, sessionId, paymentIntentId, ct);
        if (payment is null)
            return;

        payment.StripeCheckoutSessionId ??= sessionId;
        if (!string.IsNullOrWhiteSpace(paymentIntentId))
            payment.StripePaymentIntentId = paymentIntentId;

        if (!paid)
        {
            await AddHistoryAsync(payment, payment.Status, payment.Status, eventType, eventId, "Sessão concluída; pagamento ainda pendente.", null, ct);
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        if (payment.Status is PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded)
        {
            await _invoices.EnsureInvoiceReceiptAsync(payment.Id, ct);
            return;
        }

        var from = payment.Status;
        payment.Status = PaymentStatus.Succeeded;
        payment.FailureMessage = null;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        await AddHistoryAsync(payment, from, PaymentStatus.Succeeded, eventType, eventId, "Pagamento confirmado.", null, ct);
        await MarkOrderPaidAsync(payment, sessionId, ct);
        await _invoices.EnsureInvoiceReceiptAsync(payment.Id, ct);
    }

    private async Task ApplyFailureAsync(
        string eventId,
        IReadOnlyDictionary<string, string> metadata,
        string? sessionId,
        string? paymentIntentId,
        string message,
        string eventType,
        CancellationToken ct)
    {
        var payment = await FindPaymentAsync(metadata, sessionId, paymentIntentId, ct);
        if (payment is null || payment.Status is PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded)
            return;

        var from = payment.Status;
        payment.Status = PaymentStatus.Failed;
        payment.FailureMessage = message;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(paymentIntentId))
            payment.StripePaymentIntentId = paymentIntentId;
        await AddHistoryAsync(payment, from, PaymentStatus.Failed, eventType, eventId, message, null, ct);
        await _audit.LogAsync("PaymentFailed", null, payment.TenantId, nameof(Payment), payment.Id, message, ct);
    }

    private async Task ApplyChargeRefundedAsync(ChargeRefundedNotification notification, CancellationToken ct)
    {
        var payment = await _unitOfWork.Payments.FindForWebhookAsync(null, null, notification.PaymentIntentId, ct);
        if (payment is null)
            return;

        var refunded = CommissionCalculator.FromCents(notification.AmountRefundedCents);
        if (refunded > payment.Amount)
            refunded = payment.Amount;
        var from = payment.Status;
        payment.RefundedAmount = refunded;
        payment.StripeChargeId ??= notification.ChargeId;
        payment.Status = notification.FullyRefunded || refunded >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        await AddHistoryAsync(payment, from, payment.Status, notification.Type, notification.EventId, "Reembolso registado pelo Stripe.", refunded, ct);
        await SyncOrderCapturedFlagAsync(payment, ct);
        if (_stock is not null && payment.Status == PaymentStatus.Refunded)
            await _stock.ApplyRestoreForOrderAsync(payment.OrderId, StockMovementReason.OrderRefunded, ct);
        await _invoices.EnsureCreditNoteAsync(payment.Id, ct);
    }

    private async Task ApplyAccountUpdatedAsync(AccountUpdatedNotification notification, CancellationToken ct)
    {
        var tenant = await _unitOfWork.Tenants.Query()
            .FirstOrDefaultAsync(t => t.StripeAccountId == notification.AccountId, ct);
        if (tenant is null)
            return;

        tenant.StripeChargesEnabled = notification.ChargesEnabled;
        tenant.StripePayoutsEnabled = notification.PayoutsEnabled;
        tenant.StripeDetailsSubmitted = notification.DetailsSubmitted;
        tenant.StripeCardPayments = notification.CardPayments;
        tenant.StripeMbWay = notification.MbWay;
        tenant.StripeMultibanco = notification.Multibanco;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task ExecuteRefundAsync(Payment payment, decimal amount, string reason, Guid? actorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId) || string.IsNullOrWhiteSpace(payment.StripeConnectedAccountId))
            throw new InvalidOperationException("O pagamento ainda não tem um identificador Stripe para reembolso.");

        var cents = CommissionCalculator.ToCents(amount);
        var full = amount == PaymentRefundRules.Remaining(payment) && payment.RefundedAmount == 0;
        await _stripe.CreateRefundAsync(new CreateRefundCommand(
            payment.StripeConnectedAccountId,
            payment.StripePaymentIntentId,
            full ? null : cents,
            RefundApplicationFee: true), ct);

        var from = payment.Status;
        payment.RefundedAmount += amount;
        payment.Status = payment.RefundedAmount >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        await AddHistoryAsync(payment, from, payment.Status, "refund.requested", null, reason, amount, ct);
        await SyncOrderCapturedFlagAsync(payment, ct);
        if (_stock is not null && payment.Status == PaymentStatus.Refunded)
            await _stock.ApplyRestoreForOrderAsync(payment.OrderId, StockMovementReason.OrderRefunded, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        if (_stock is not null)
            await _stock.FlushNoticesAsync(ct);
        await _audit.LogAsync("PaymentRefunded", actorId, payment.TenantId, nameof(Payment), payment.Id, $"{amount} {reason}", ct);
        await _invoices.EnsureCreditNoteAsync(payment.Id, ct);
    }

    private async Task MarkOrderPaidAsync(Payment payment, string sessionId, CancellationToken ct)
    {
        var order = await _unitOfWork.Orders.Query()
            .IgnoreQueryFilters()
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == payment.OrderId, ct);
        if (order is null)
            return;

        var already = order.PaymentCaptured;
        order.PaymentCaptured = true;
        order.PaymentReference = sessionId;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _audit.LogAsync("OrderPaymentCaptured", null, order.TenantId, nameof(Order), order.Id, sessionId, ct);
        if (already)
            return;

        var tenant = await _unitOfWork.Tenants.Query().FirstOrDefaultAsync(t => t.Id == order.TenantId, ct);
        if (tenant is null)
            return;
        _emails.PaymentReceived(order, tenant);
        _emails.PaymentReceivedFlorist(order, tenant);
        _push.OrderPaid(order, tenant);
        await _panel.RecordPaymentReceivedAsync(order.TenantId, order.Id, ct);
    }

    private async Task SyncOrderCapturedFlagAsync(Payment payment, CancellationToken ct)
    {
        var order = await _unitOfWork.Orders.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == payment.OrderId, ct);
        if (order is null)
            return;
        order.PaymentCaptured = payment.Status is PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded;
        order.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task<Payment?> FindPaymentAsync(
        IReadOnlyDictionary<string, string> metadata,
        string? sessionId,
        string? paymentIntentId,
        CancellationToken ct)
    {
        Guid? paymentId = metadata.TryGetValue("paymentId", out var raw) && Guid.TryParse(raw, out var id) ? id : null;
        return await _unitOfWork.Payments.FindForWebhookAsync(paymentId, sessionId, paymentIntentId, ct);
    }

    private async Task<PlatformBillingSettings> GetOrCreateSettingsAsync(CancellationToken ct)
    {
        var settings = await _unitOfWork.BillingSettings.Query()
            .FirstOrDefaultAsync(s => s.Id == PlatformBillingSettings.SingletonId, ct);
        if (settings is not null)
            return settings;

        settings = new PlatformBillingSettings
        {
            Id = PlatformBillingSettings.SingletonId,
            CommissionPercent = PlatformBillingSettings.DefaultCommissionPercent,
            MonthlyFeeAmount = 0
        };
        await _unitOfWork.BillingSettings.AddAsync(settings, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return settings;
    }

    private async Task<Tenant> RequireCurrentTenantAsync(CancellationToken ct)
    {
        var id = RequireTenantId();
        return await RequireTenantEntityAsync(id, ct);
    }

    private Guid RequireTenantId() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");

    private async Task<Tenant> RequireTenantEntityAsync(Guid id, CancellationToken ct) =>
        await _unitOfWork.Tenants.Query().FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new InvalidOperationException("Loja não encontrada.");

    private string RequireUrl(string? candidate, string relativePath)
    {
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            EnsureAbsoluteUrl(candidate);
            return candidate;
        }

        var baseUrl = _emailOptions.WebBaseUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Indique o URL de retorno do pagamento.");
        return baseUrl + relativePath;
    }

    private static void EnsureAbsoluteUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("https" or "http" or "floristhub"))
            throw new InvalidOperationException("URL de retorno inválido.");
    }

    private static void ApplySnapshot(Tenant tenant, ConnectedAccountSnapshot snapshot)
    {
        tenant.StripeChargesEnabled = snapshot.ChargesEnabled;
        tenant.StripePayoutsEnabled = snapshot.PayoutsEnabled;
        tenant.StripeDetailsSubmitted = snapshot.DetailsSubmitted;
        tenant.StripeCardPayments = snapshot.CardPayments;
        tenant.StripeMbWay = snapshot.MbWay;
        tenant.StripeMultibanco = snapshot.Multibanco;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private StripeConnectStatusDto MapConnect(Tenant tenant) =>
        new(
            _stripe.IsConfigured,
            !string.IsNullOrWhiteSpace(tenant.StripeAccountId),
            tenant.StripeChargesEnabled,
            tenant.StripePayoutsEnabled,
            tenant.StripeDetailsSubmitted,
            tenant.StripeCardPayments,
            tenant.StripeMbWay,
            tenant.StripeMultibanco,
            tenant.StripeAccountId,
            string.IsNullOrWhiteSpace(_stripeOptions.PublishableKey) ? null : _stripeOptions.PublishableKey);

    private static TenantBillingDto MapTenantBilling(Tenant tenant, PlatformBillingSettings settings) =>
        new(
            tenant.CommissionPercentOverride,
            tenant.MonthlyFeeAmount,
            tenant.CommissionPercentOverride ?? settings.CommissionPercent,
            tenant.MonthlyFeeAmount ?? settings.MonthlyFeeAmount);

    private static PaymentDto Map(Payment payment) =>
        new(
            payment.Id,
            payment.OrderId,
            payment.Status,
            payment.Amount,
            payment.RefundedAmount,
            payment.CommissionPercent,
            payment.ApplicationFeeAmountCents,
            payment.Currency,
            payment.FailureMessage,
            payment.CreatedAt,
            payment.Events
                .OrderBy(e => e.CreatedAt)
                .Select(e => new PaymentEventDto(e.Id, e.EventType, e.FromStatus, e.ToStatus, e.Amount, e.Notes, e.CreatedAt))
                .ToList());

    private async Task AddHistoryAsync(
        Payment payment,
        PaymentStatus? from,
        PaymentStatus to,
        string eventType,
        string? stripeEventId,
        string? notes,
        decimal? amount,
        CancellationToken ct)
    {
        var ev = History(payment, from, to, eventType, stripeEventId, notes, amount);
        await _unitOfWork.PaymentEvents.AddAsync(ev, ct);
    }

    private static PaymentEvent History(
        Payment payment,
        PaymentStatus? from,
        PaymentStatus to,
        string eventType,
        string? stripeEventId,
        string? notes,
        decimal? amount = null) =>
        new()
        {
            TenantId = payment.TenantId,
            PaymentId = payment.Id,
            EventType = eventType,
            StripeEventId = stripeEventId,
            FromStatus = from,
            ToStatus = to,
            Amount = amount,
            Notes = notes
        };
}
