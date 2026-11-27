using FloristHub.DAL.Entities;
using FloristHub.Services.Interfaces.Email;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Email;

public sealed class RecurringEmailNotificationsService : IRecurringEmailNotifications
{
    private readonly IEmailQueue _queue;
    private readonly EmailOptions _options;

    public RecurringEmailNotificationsService(IEmailQueue queue, IOptions<EmailOptions> options)
    {
        _queue = queue;
        _options = options.Value;
    }

    public void SubscriptionRequested(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        EnqueueFlorist(tenant, $"Nova recorrência de {customer.FullName}",
            $"O cliente {customer.Email} pediu uma entrega {DescribeFrequency(subscription.Frequency)}.");

    public void SubscriptionAccepted(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        EnqueueCustomer(customer, "Recorrência aceite",
            $"A florista {tenant.Name} aceitou a sua subscrição de flores.");

    public void SubscriptionRejected(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        EnqueueCustomer(customer, "Recorrência recusada",
            $"A florista {tenant.Name} não pôde aceitar a recorrência.");

    public void SubscriptionPaused(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        NotifyBoth(subscription, tenant, customer, "Recorrência em pausa", "A recorrência foi colocada em pausa.");

    public void SubscriptionResumed(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        NotifyBoth(subscription, tenant, customer, "Recorrência reativada", "A recorrência voltou a estar ativa.");

    public void SubscriptionCancelled(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        NotifyBoth(subscription, tenant, customer, "Recorrência cancelada", "A subscrição de flores foi cancelada.");

    public void DeliverySkipped(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer, DateTimeOffset skippedAt) =>
        NotifyBoth(subscription, tenant, customer, "Entrega saltada",
            $"A entrega de {skippedAt.LocalDateTime:dd/MM/yyyy} foi saltada.");

    public void OrderGenerated(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer, Order order) =>
        NotifyBoth(subscription, tenant, customer, "Encomenda gerada da recorrência",
            $"Foi criada a encomenda da entrega de {order.RequestedDeliveryAt:dd/MM/yyyy}. Confirme e pague no habitual.");

    public void CapacityUnavailable(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer, DateTimeOffset deliveryAt) =>
        NotifyBoth(subscription, tenant, customer, "Recorrência sem vaga",
            $"A entrega de {deliveryAt.LocalDateTime:dd/MM/yyyy} não foi criada porque a loja não tem vaga. A próxima data da recorrência foi avançada.");

    private void NotifyBoth(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer, string subject, string body)
    {
        EnqueueCustomer(customer, subject, body);
        EnqueueFlorist(tenant, subject, body);
    }

    private void EnqueueCustomer(ApplicationUser customer, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(customer.Email))
            return;
        Enqueue(customer.Email, subject, BodyHtml(body), $"Recurring:Customer");
    }

    private void EnqueueFlorist(Tenant tenant, string subject, string body)
    {
        var email = tenant.ContactEmail;
        if (string.IsNullOrWhiteSpace(email))
            return;
        Enqueue(email, subject, BodyHtml(body), "Recurring:Florist");
    }

    private void Enqueue(string to, string subject, string html, string auditContext) =>
        _ = _queue.EnqueueAsync(new QueuedEmail(to, subject, html, ReplyTo: null, AuditContext: auditContext));

    private static string BodyHtml(string text) => $"<p style=\"font-family:sans-serif;\">{System.Net.WebUtility.HtmlEncode(text)}</p>";

    private static string DescribeFrequency(DAL.Enums.RecurringFrequency frequency) => frequency switch
    {
        DAL.Enums.RecurringFrequency.Weekly => "semanal",
        DAL.Enums.RecurringFrequency.Biweekly => "quinzenal",
        DAL.Enums.RecurringFrequency.Monthly => "mensal",
        _ => "periódica"
    };
}
