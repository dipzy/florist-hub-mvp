using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.Services.Interfaces.Push;
using Microsoft.Extensions.Logging;

namespace FloristHub.Services.Push;

public sealed class OrderPushNotifications : IOrderPushNotifications
{
    private readonly IPushQueue _queue;
    private readonly ILogger<OrderPushNotifications> _logger;

    public OrderPushNotifications(IPushQueue queue, ILogger<OrderPushNotifications> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public void OrderCreated(Order order, Tenant tenant) =>
        SafeEnqueue(new PushWorkItem(
            order.TenantId,
            PushAudience.TenantFlorists,
            null,
            order.Id,
            "Nova encomenda",
            "Entrou uma encomenda nova.",
            "new-order",
            NotificationPreferenceKind.NewOrder));

    public void OrderPaid(Order order, Tenant tenant) =>
        SafeEnqueue(new PushWorkItem(
            order.TenantId,
            PushAudience.TenantFlorists,
            null,
            order.Id,
            "Encomenda paga",
            "O pagamento da encomenda foi recebido.",
            "order-paid",
            NotificationPreferenceKind.OrderPaid));

    public void OrderStatusChanged(Order order, Tenant tenant, OrderStatus status)
    {
        if (!TryDescribe(status, out var title, out var body))
            return;

        SafeEnqueue(new PushWorkItem(
            order.TenantId,
            PushAudience.Customer,
            order.CustomerId,
            order.Id,
            title,
            body,
            "order-status",
            NotificationPreferenceKind.OrderStatus));
    }

    public static bool TryDescribe(OrderStatus status, out string title, out string body)
    {
        (title, body) = status switch
        {
            OrderStatus.Confirmed => ("Encomenda confirmada", "A florista confirmou a sua encomenda."),
            OrderStatus.InPreparation => ("Encomenda em preparação", "A sua encomenda está em preparação."),
            OrderStatus.Ready => ("Encomenda pronta", "A sua encomenda está pronta."),
            OrderStatus.Delivered => ("Encomenda entregue", "A sua encomenda foi entregue."),
            _ => (string.Empty, string.Empty)
        };
        return title.Length > 0;
    }

    private void SafeEnqueue(PushWorkItem item)
    {
        try
        {
            _queue.Enqueue(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível enfileirar push {Type} da encomenda {OrderId}", item.Type, item.OrderId);
        }
    }
}
