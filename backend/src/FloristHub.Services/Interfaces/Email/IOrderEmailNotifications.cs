using FloristHub.DAL.Entities;

namespace FloristHub.Services.Interfaces.Email;

public interface IOrderEmailNotifications
{
    void OrderCreated(Order order, Tenant tenant);
    void OrderConfirmed(Order order, Tenant tenant);
    void OrderInPreparation(Order order, Tenant tenant);
    void OrderRejected(Order order, Tenant tenant, string reason);
    void ChangeProposalSent(Order order, Tenant tenant, OrderChangeProposal proposal);
    void PaymentReceived(Order order, Tenant tenant);
    void PaymentReceivedFlorist(Order order, Tenant tenant);
    void OrderReady(Order order, Tenant tenant);
    void OrderDelivered(Order order, Tenant tenant);
    void OrderCancelled(Order order, Tenant tenant, string? reason);
}
