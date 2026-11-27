using FloristHub.DAL.Entities;
using FloristHub.Services.Interfaces.Push;
using Microsoft.Extensions.Logging;

namespace FloristHub.Services.Push;

public sealed class RecurringPushNotifications : IRecurringPushNotifications
{
    private readonly IPushQueue _queue;
    private readonly ILogger<RecurringPushNotifications> _logger;

    public RecurringPushNotifications(IPushQueue queue, ILogger<RecurringPushNotifications> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public void SubscriptionRequested(RecurringSubscription subscription, Tenant tenant) =>
        EnqueueFlorist(subscription, "Nova recorrência", "Um cliente pediu uma subscrição de flores.");

    public void SubscriptionAccepted(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        EnqueueCustomer(subscription, customer, "Recorrência aceite", "A florista aceitou a sua subscrição.");

    public void SubscriptionRejected(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        EnqueueCustomer(subscription, customer, "Recorrência recusada", "A florista recusou a subscrição.");

    public void SubscriptionPaused(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        EnqueueBoth(subscription, customer, "Recorrência em pausa", "A subscrição foi colocada em pausa.");

    public void SubscriptionResumed(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        EnqueueBoth(subscription, customer, "Recorrência reativada", "A subscrição voltou a estar ativa.");

    public void SubscriptionCancelled(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        EnqueueBoth(subscription, customer, "Recorrência cancelada", "A subscrição foi cancelada.");

    public void DeliverySkipped(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer) =>
        EnqueueBoth(subscription, customer, "Entrega saltada", "Saltou a próxima entrega da recorrência.");

    public void OrderGenerated(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer, Order order) =>
        EnqueueBoth(subscription, customer, "Encomenda da recorrência",
            $"Nova encomenda para {order.RequestedDeliveryAt:dd/MM}.", order.Id);

    public void CapacityUnavailable(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer, DateTimeOffset deliveryAt) =>
        EnqueueBoth(subscription, customer, "Recorrência sem vaga",
            $"A entrega de {deliveryAt:dd/MM} não foi criada: a loja não tem vaga.");

    private void EnqueueBoth(RecurringSubscription subscription, ApplicationUser customer, string title, string body, Guid? orderId = null)
    {
        EnqueueCustomer(subscription, customer, title, body, orderId);
        EnqueueFlorist(subscription, title, body, orderId);
    }

    private void EnqueueCustomer(RecurringSubscription subscription, ApplicationUser customer, string title, string body, Guid? orderId = null) =>
        SafeEnqueue(new PushWorkItem(
            subscription.TenantId,
            PushAudience.Customer,
            customer.Id,
            orderId ?? Guid.Empty,
            title,
            body,
            "recurring",
            NotificationPreferenceKind.RecurringSubscriptions));

    private void EnqueueFlorist(RecurringSubscription subscription, string title, string body, Guid? orderId = null) =>
        SafeEnqueue(new PushWorkItem(
            subscription.TenantId,
            PushAudience.TenantFlorists,
            null,
            orderId ?? Guid.Empty,
            title,
            body,
            "recurring",
            NotificationPreferenceKind.RecurringSubscriptions));

    private void SafeEnqueue(PushWorkItem item)
    {
        try
        {
            _queue.Enqueue(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível enfileirar push de recorrência {Type}", item.Type);
        }
    }
}
