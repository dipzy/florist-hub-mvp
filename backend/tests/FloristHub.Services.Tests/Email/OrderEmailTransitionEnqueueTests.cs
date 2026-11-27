using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.Services.Email;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Tests.Email;

/// <summary>
/// Mapeia cada transição de negócio ao email enfileirado (como o OrderService invoca IOrderEmailNotifications).
/// </summary>
public class OrderEmailTransitionEnqueueTests
{
    [Theory]
    [InlineData(nameof(OrderEmailNotificationsService.OrderCreated), 2)]
    [InlineData(nameof(OrderEmailNotificationsService.OrderConfirmed), 1)]
    [InlineData(nameof(OrderEmailNotificationsService.OrderRejected), 1)]
    [InlineData(nameof(OrderEmailNotificationsService.OrderReady), 1)]
    [InlineData(nameof(OrderEmailNotificationsService.OrderDelivered), 1)]
    [InlineData(nameof(OrderEmailNotificationsService.OrderCancelled), 1)]
    [InlineData(nameof(OrderEmailNotificationsService.PaymentReceived), 1)]
    public void Transition_hooks_enqueue_expected_message_count(string method, int expectedCount)
    {
        var queue = new FakeEmailQueue();
        var sut = new OrderEmailNotificationsService(queue, Options.Create(new EmailOptions()));
        var (order, tenant) = Sample();

        switch (method)
        {
            case nameof(OrderEmailNotificationsService.OrderCreated):
                sut.OrderCreated(order, tenant);
                break;
            case nameof(OrderEmailNotificationsService.OrderConfirmed):
                sut.OrderConfirmed(order, tenant);
                break;
            case nameof(OrderEmailNotificationsService.OrderRejected):
                sut.OrderRejected(order, tenant, "motivo");
                break;
            case nameof(OrderEmailNotificationsService.OrderReady):
                sut.OrderReady(order, tenant);
                break;
            case nameof(OrderEmailNotificationsService.OrderDelivered):
                sut.OrderDelivered(order, tenant);
                break;
            case nameof(OrderEmailNotificationsService.OrderCancelled):
                sut.OrderCancelled(order, tenant, "motivo");
                break;
            case nameof(OrderEmailNotificationsService.PaymentReceived):
                order.PaymentCaptured = true;
                sut.PaymentReceived(order, tenant);
                break;
        }

        Assert.Equal(expectedCount, queue.Messages.Count);
    }

    private static (Order Order, Tenant Tenant) Sample()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Loja", Slug = "loja", ContactEmail = "f@loja.pt" };
        var customer = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "c@loja.pt",
            UserName = "c@loja.pt",
            TenantId = tenantId
        };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customer.Id,
            Customer = customer,
            EstimatedTotal = 10m,
            PaymentCaptured = true
        };
        return (order, tenant);
    }
}
