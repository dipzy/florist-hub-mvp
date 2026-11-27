using FloristHub.DAL.Configurations;
using FloristHub.DAL.Entities;
using FloristHub.DAL.MultiTenancy;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.DAL.Persistence;

public class ApplicationDbContext :
    IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>,
    IApplicationDbContext,
    IDataProtectionKeyContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>Usado pelos filtros globais EF (avaliado em cada consulta no contexto atual).</summary>
    internal Guid? CurrentTenantId => _tenantContext.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Flower> Flowers => Set<Flower>();
    public DbSet<PortfolioArrangement> PortfolioArrangements => Set<PortfolioArrangement>();
    public DbSet<PortfolioArrangementPhoto> PortfolioArrangementPhotos => Set<PortfolioArrangementPhoto>();
    public DbSet<PortfolioArrangementItem> PortfolioArrangementItems => Set<PortfolioArrangementItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<OrderChangeProposal> OrderChangeProposals => Set<OrderChangeProposal>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TenantOpeningHour> TenantOpeningHours => Set<TenantOpeningHour>();
    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<PlatformBillingSettings> PlatformBillingSettings => Set<PlatformBillingSettings>();
    public DbSet<StripeWebhookReceipt> StripeWebhookReceipts => Set<StripeWebhookReceipt>();
    public DbSet<PushDevice> PushDevices => Set<PushDevice>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<LegalDocumentVersion> LegalDocumentVersions => Set<LegalDocumentVersion>();
    public DbSet<FloristInvoicingSettings> FloristInvoicingSettings => Set<FloristInvoicingSettings>();
    public DbSet<SalesDocument> SalesDocuments => Set<SalesDocument>();
    public DbSet<FloristInvite> FloristInvites => Set<FloristInvite>();
    public DbSet<FloristTenantMembership> FloristTenantMemberships => Set<FloristTenantMembership>();
    public DbSet<DeliveryCapacityPolicy> DeliveryCapacityPolicies => Set<DeliveryCapacityPolicy>();
    public DbSet<DeliveryCapacityWindow> DeliveryCapacityWindows => Set<DeliveryCapacityWindow>();
    public DbSet<DeliveryCapacityException> DeliveryCapacityExceptions => Set<DeliveryCapacityException>();
    public DbSet<DeliveryCapacityHold> DeliveryCapacityHolds => Set<DeliveryCapacityHold>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponTarget> CouponTargets => Set<CouponTarget>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<SeasonalCampaign> SeasonalCampaigns => Set<SeasonalCampaign>();
    public DbSet<CampaignFlower> CampaignFlowers => Set<CampaignFlower>();
    public DbSet<OrderReview> OrderReviews => Set<OrderReview>();
    public DbSet<FloristPanelNotification> FloristPanelNotifications => Set<FloristPanelNotification>();
    public DbSet<RecurringSubscription> RecurringSubscriptions => Set<RecurringSubscription>();
    public DbSet<RecurringSubscriptionItem> RecurringSubscriptionItems => Set<RecurringSubscriptionItem>();
    public DbSet<RecurringOccurrence> RecurringOccurrences => Set<RecurringOccurrence>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<Flower>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<PortfolioArrangement>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<Order>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<OrderItem>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<OrderStatusHistory>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<OrderChangeProposal>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<PortfolioArrangementPhoto>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<PortfolioArrangementItem>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<TenantOpeningHour>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<DeliveryZone>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<Payment>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<PaymentEvent>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<PushDevice>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<NotificationPreference>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<FloristInvoicingSettings>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<SalesDocument>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<DeliveryCapacityPolicy>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<DeliveryCapacityWindow>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<DeliveryCapacityException>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<DeliveryCapacityHold>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<StockMovement>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<Coupon>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<CouponTarget>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<CouponRedemption>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<SeasonalCampaign>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<CampaignFlower>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<OrderReview>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<FloristPanelNotification>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<RecurringSubscription>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<RecurringSubscriptionItem>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
        builder.Entity<RecurringOccurrence>().HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId);
    }
}
