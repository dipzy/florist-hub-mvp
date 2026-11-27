using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.Services.Email;
using FloristHub.Services.Orders;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Tests.Email;

public class OrderEmailNotificationsServiceTests
{
    private readonly FakeEmailQueue _queue = new();
    private readonly OrderEmailNotificationsService _sut;

    public OrderEmailNotificationsServiceTests() =>
        _sut = new OrderEmailNotificationsService(_queue, Options.Create(new EmailOptions
        {
            WebBaseUrl = "https://demo.pages.dev"
        }));

    [Fact]
    public void Order_created_enqueues_customer_and_florist_emails()
    {
        var (order, tenant) = BuildOrder();

        _sut.OrderCreated(order, tenant);

        Assert.Equal(2, _queue.Messages.Count);
        Assert.Contains(_queue.Messages, m => m.To == "cliente@demo.pt" && m.Subject.Contains("recebida", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(_queue.Messages, m => m.To == "florista@demo.pt" && m.Subject.Contains("Nova encomenda", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Order_created_customer_email_includes_tracking_link()
    {
        var (order, tenant) = BuildOrder();
        order.TrackingAccessToken = OrderTrackingToken.Create();

        _sut.OrderCreated(order, tenant);

        var customer = Assert.Single(_queue.Messages.Where(m => m.To == "cliente@demo.pt"));
        Assert.Contains("https://demo.pages.dev/acompanhar/", customer.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_confirmed_includes_payment_link()
    {
        var (order, tenant) = BuildOrder();

        _sut.OrderConfirmed(order, tenant);

        var msg = Assert.Single(_queue.Messages);
        Assert.Equal("cliente@demo.pt", msg.To);
        Assert.Contains("confirmada", msg.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://demo.pages.dev/encomendas/", msg.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Change_proposal_enqueues_customer_email()
    {
        var (order, tenant) = BuildOrder();
        var proposal = new OrderChangeProposal
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            OrderId = order.Id,
            Message = "Substituir rosas por lírios",
            Status = OrderChangeProposalStatus.Pending
        };

        _sut.ChangeProposalSent(order, tenant, proposal);

        var msg = Assert.Single(_queue.Messages);
        Assert.Contains("alteração", msg.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Substituir rosas", msg.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Status_notifications_enqueue_expected_subjects()
    {
        var (order, tenant) = BuildOrder();

        _sut.OrderRejected(order, tenant, "Sem stock");
        order.PaymentCaptured = true;
        _sut.PaymentReceived(order, tenant);
        _sut.OrderInPreparation(order, tenant);
        _sut.OrderReady(order, tenant);
        _sut.OrderDelivered(order, tenant);
        _sut.OrderCancelled(order, tenant, "Cliente desistiu");

        Assert.Equal(6, _queue.Messages.Count);
        Assert.Contains(_queue.Messages, m => m.Subject.Contains("preparação", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(_queue.Messages, m => m.Subject.Contains("recusada", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(_queue.Messages, m => m.Subject.Contains("Pagamento", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(_queue.Messages, m => m.Subject.Contains("pronta", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(_queue.Messages, m => m.Subject.Contains("entregue", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(_queue.Messages, m => m.Subject.Contains("cancelada", StringComparison.OrdinalIgnoreCase));
    }

    private static (Order Order, Tenant Tenant) BuildOrder()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Flores da Lina",
            Slug = "flores-lina",
            ContactEmail = "florista@demo.pt",
            LogoUrl = "https://cdn.example/logo.png"
        };
        var customer = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "cliente@demo.pt",
            FullName = "Ana Costa",
            UserName = "cliente@demo.pt",
            TenantId = tenantId
        };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customer.Id,
            Customer = customer,
            Status = OrderStatus.PendingConfirmation,
            DeliveryType = DeliveryType.StorePickup,
            EstimatedTotal = 42.5m,
            PaymentCaptured = true,
            Items =
            {
                new OrderItem { TenantId = tenantId, Name = "Rosa vermelha", Quantity = 12, UnitPrice = 1.5m }
            }
        };
        return (order, tenant);
    }
}
