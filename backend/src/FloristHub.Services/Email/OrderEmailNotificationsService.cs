using FloristHub.DAL.Entities;
using FloristHub.Services.Email.Templates;
using FloristHub.Services.Interfaces.Email;
using FloristHub.Services.Orders;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Email;

public sealed class OrderEmailNotificationsService : IOrderEmailNotifications
{
    private readonly IEmailQueue _queue;
    private readonly EmailOptions _options;

    public OrderEmailNotificationsService(IEmailQueue queue, IOptions<EmailOptions> options)
    {
        _queue = queue;
        _options = options.Value;
    }

    public void OrderCreated(Order order, Tenant tenant)
    {
        var view = ToView(order);
        if (!string.IsNullOrWhiteSpace(view.CustomerEmail))
        {
            var (subject, html) = TransactionalEmailTemplates.OrderReceivedCustomer(tenant, view);
            Enqueue(view.CustomerEmail, subject, html, $"OrderCreated:Customer:{order.Id}");
        }

        var floristEmail = ResolveFloristNotificationEmail(tenant);
        if (!string.IsNullOrWhiteSpace(floristEmail))
        {
            var (subject, html) = TransactionalEmailTemplates.NewOrderFlorist(tenant, view);
            Enqueue(floristEmail, subject, html, $"OrderCreated:Florist:{order.Id}");
        }
    }

    public void OrderConfirmed(Order order, Tenant tenant)
    {
        var email = order.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        var view = ToView(order);
        var paymentUrl = BuildPaymentUrl(order.Id);
        var (subject, html) = TransactionalEmailTemplates.OrderConfirmed(tenant, view, paymentUrl);
        Enqueue(email, subject, html, $"OrderConfirmed:{order.Id}");
    }

    public void OrderRejected(Order order, Tenant tenant, string reason)
    {
        var email = order.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        var view = ToView(order);
        var (subject, html) = TransactionalEmailTemplates.OrderRejected(tenant, view, reason);
        Enqueue(email, subject, html, $"OrderRejected:{order.Id}");
    }

    public void ChangeProposalSent(Order order, Tenant tenant, OrderChangeProposal proposal)
    {
        var email = order.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        var view = ToView(order);
        var (subject, html) = TransactionalEmailTemplates.ChangeProposal(tenant, view, proposal.Message, proposal.RevisedTotal);
        Enqueue(email, subject, html, $"OrderChangeProposal:{proposal.Id}");
    }

    public void PaymentReceived(Order order, Tenant tenant)
    {
        if (order.PaymentCaptured is false)
            return;

        var email = order.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        var view = ToView(order);
        var (subject, html) = TransactionalEmailTemplates.PaymentReceived(tenant, view);
        Enqueue(email, subject, html, $"OrderPaymentReceived:{order.Id}");
    }

    public void PaymentReceivedFlorist(Order order, Tenant tenant)
    {
        if (order.PaymentCaptured is false)
            return;

        var email = ResolveFloristNotificationEmail(tenant);
        if (string.IsNullOrWhiteSpace(email))
            return;

        var view = ToView(order);
        var (subject, html) = TransactionalEmailTemplates.PaymentReceivedFlorist(tenant, view);
        Enqueue(email, subject, html, $"OrderPaymentReceivedFlorist:{order.Id}");
    }

    public void OrderInPreparation(Order order, Tenant tenant)
    {
        var email = order.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        var view = ToView(order);
        var (subject, html) = TransactionalEmailTemplates.OrderInPreparation(tenant, view);
        Enqueue(email, subject, html, $"OrderInPreparation:{order.Id}");
    }

    public void OrderReady(Order order, Tenant tenant)
    {
        var email = order.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        var view = ToView(order);
        var (subject, html) = TransactionalEmailTemplates.OrderReady(tenant, view);
        Enqueue(email, subject, html, $"OrderReady:{order.Id}");
    }

    public void OrderDelivered(Order order, Tenant tenant)
    {
        var email = order.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        var view = ToView(order);
        var (subject, html) = TransactionalEmailTemplates.OrderDelivered(tenant, view);
        Enqueue(email, subject, html, $"OrderDelivered:{order.Id}");
    }

    public void OrderCancelled(Order order, Tenant tenant, string? reason)
    {
        var email = order.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        var view = ToView(order);
        var (subject, html) = TransactionalEmailTemplates.OrderCancelled(tenant, view, reason);
        Enqueue(email, subject, html, $"OrderCancelled:{order.Id}");
    }

    private void Enqueue(string to, string subject, string html, string auditContext) =>
        _ = _queue.EnqueueAsync(new QueuedEmail(to, subject, html, ReplyTo: null, AuditContext: auditContext));

    private string? BuildPaymentUrl(Guid orderId)
    {
        var baseUrl = _options.WebBaseUrl?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl}/encomendas/{orderId}";
    }

    private string? BuildTrackingUrl(Order order)
    {
        if (!OrderTrackingToken.LooksValid(order.TrackingAccessToken))
            return null;
        var baseUrl = _options.WebBaseUrl?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl}/acompanhar/{order.TrackingAccessToken}";
    }

    private OrderEmailView ToView(Order order) => TransactionalEmailTemplates.ToView(order, BuildTrackingUrl(order));

    private static string? ResolveFloristNotificationEmail(Tenant tenant) =>
        string.IsNullOrWhiteSpace(tenant.ContactEmail) ? null : tenant.ContactEmail.Trim();
}
