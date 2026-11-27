using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;

namespace FloristHub.Services.Interfaces.Push;

public interface IOrderPushNotifications
{
    void OrderCreated(Order order, Tenant tenant);
    void OrderPaid(Order order, Tenant tenant);
    void OrderStatusChanged(Order order, Tenant tenant, OrderStatus status);
}
