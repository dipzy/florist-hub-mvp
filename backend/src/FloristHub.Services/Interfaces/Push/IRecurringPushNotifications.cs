using FloristHub.DAL.Entities;

namespace FloristHub.Services.Interfaces.Push;

public interface IRecurringPushNotifications
{
    void SubscriptionRequested(RecurringSubscription subscription, Tenant tenant);
    void SubscriptionAccepted(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer);
    void SubscriptionRejected(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer);
    void SubscriptionPaused(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer);
    void SubscriptionResumed(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer);
    void SubscriptionCancelled(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer);
    void DeliverySkipped(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer);
    void OrderGenerated(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer, Order order);
    void CapacityUnavailable(RecurringSubscription subscription, Tenant tenant, ApplicationUser customer, DateTimeOffset deliveryAt);
}
