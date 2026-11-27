using FloristHub.DAL.Entities;
using FloristHub.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace FloristHub.DAL.UnitOfWork;

public interface IUnitOfWork
{
    IFlowerRepository Flowers { get; }
    IOrderRepository Orders { get; }
    IPortfolioRepository Portfolio { get; }
    ITenantRepository Tenants { get; }
    IRepository<DeliveryZone> DeliveryZones { get; }
    IRepository<OrderChangeProposal> OrderChangeProposals { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IPaymentRepository Payments { get; }
    IRepository<PaymentEvent> PaymentEvents { get; }
    IRepository<PlatformBillingSettings> BillingSettings { get; }
    IRepository<StripeWebhookReceipt> WebhookReceipts { get; }
    IRepository<LegalDocumentVersion> LegalDocumentVersions { get; }
    IRepository<FloristInvoicingSettings> InvoicingSettings { get; }
    IRepository<SalesDocument> SalesDocuments { get; }
    IRepository<FloristInvite> FloristInvites { get; }
    IRepository<FloristTenantMembership> FloristTenantMemberships { get; }
    IRepository<DeliveryCapacityPolicy> CapacityPolicies { get; }
    IRepository<DeliveryCapacityWindow> CapacityWindows { get; }
    IRepository<DeliveryCapacityException> CapacityExceptions { get; }
    IRepository<DeliveryCapacityHold> CapacityHolds { get; }
    IRepository<StockMovement> StockMovements { get; }
    IRepository<Coupon> Coupons { get; }
    IRepository<CouponTarget> CouponTargets { get; }
    IRepository<CouponRedemption> CouponRedemptions { get; }
    IRepository<SeasonalCampaign> Campaigns { get; }
    IRepository<CampaignFlower> CampaignFlowers { get; }
    IRepository<OrderReview> Reviews { get; }
    IRepository<FloristPanelNotification> FloristPanelNotifications { get; }
    IRepository<RecurringSubscription> RecurringSubscriptions { get; }
    IRepository<RecurringSubscriptionItem> RecurringSubscriptionItems { get; }
    IRepository<RecurringOccurrence> RecurringOccurrences { get; }

    void Add<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Corre a ação numa transação retriável, ou na transação já aberta.</summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);

    /// <summary>Bloqueia o cupão até ao fim da transação corrente, para o máximo de utilizações.</summary>
    Task LockCouponAsync(Guid tenantId, Guid couponId, CancellationToken cancellationToken = default);
}
