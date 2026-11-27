using FloristHub.DAL.Entities;

namespace FloristHub.DAL.Configurations;

/// <summary>
/// Lista de entidades com <c>TenantId</c> e filtro global.
/// Os filtros estão em <see cref="Persistence.ApplicationDbContext"/>, avaliados pelo contexto atual.
/// </summary>
public static class TenantQueryFilterConfigurer
{
    public static readonly Type[] TenantScopedEntityTypes =
    [
        typeof(Flower),
        typeof(PortfolioArrangement),
        typeof(Order),
        typeof(OrderItem),
        typeof(OrderStatusHistory),
        typeof(OrderChangeProposal),
        typeof(PortfolioArrangementPhoto),
        typeof(PortfolioArrangementItem),
        typeof(TenantOpeningHour),
        typeof(DeliveryZone),
        typeof(Payment),
        typeof(PaymentEvent),
        typeof(PushDevice),
        typeof(NotificationPreference),
        typeof(FloristInvoicingSettings),
        typeof(SalesDocument),
        typeof(DeliveryCapacityPolicy),
        typeof(DeliveryCapacityWindow),
        typeof(DeliveryCapacityException),
        typeof(DeliveryCapacityHold),
        typeof(StockMovement),
        typeof(Coupon),
        typeof(CouponTarget),
        typeof(CouponRedemption),
        typeof(SeasonalCampaign),
        typeof(CampaignFlower),
        typeof(OrderReview),
        typeof(FloristPanelNotification),
        typeof(RecurringSubscription),
        typeof(RecurringSubscriptionItem),
        typeof(RecurringOccurrence),
    ];
}
