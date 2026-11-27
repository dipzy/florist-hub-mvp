using FloristHub.DAL.Entities;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.Repositories;
using FloristHub.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FloristHub.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Flowers = new FlowerRepository(context);
        Orders = new OrderRepository(context);
        Portfolio = new PortfolioRepository(context);
        Tenants = new TenantRepository(context);
        DeliveryZones = new Repository<DeliveryZone>(context);
        OrderChangeProposals = new Repository<OrderChangeProposal>(context);
        AuditLogs = new Repository<AuditLog>(context);
        Payments = new PaymentRepository(context);
        PaymentEvents = new Repository<PaymentEvent>(context);
        BillingSettings = new Repository<PlatformBillingSettings>(context);
        WebhookReceipts = new Repository<StripeWebhookReceipt>(context);
        LegalDocumentVersions = new Repository<LegalDocumentVersion>(context);
        InvoicingSettings = new Repository<FloristInvoicingSettings>(context);
        SalesDocuments = new Repository<SalesDocument>(context);
        FloristInvites = new Repository<FloristInvite>(context);
        FloristTenantMemberships = new Repository<FloristTenantMembership>(context);
        CapacityPolicies = new Repository<DeliveryCapacityPolicy>(context);
        CapacityWindows = new Repository<DeliveryCapacityWindow>(context);
        CapacityExceptions = new Repository<DeliveryCapacityException>(context);
        CapacityHolds = new Repository<DeliveryCapacityHold>(context);
        StockMovements = new Repository<StockMovement>(context);
        Coupons = new Repository<Coupon>(context);
        CouponTargets = new Repository<CouponTarget>(context);
        CouponRedemptions = new Repository<CouponRedemption>(context);
        Campaigns = new Repository<SeasonalCampaign>(context);
        CampaignFlowers = new Repository<CampaignFlower>(context);
        Reviews = new Repository<OrderReview>(context);
        FloristPanelNotifications = new Repository<FloristPanelNotification>(context);
        RecurringSubscriptions = new Repository<RecurringSubscription>(context);
        RecurringSubscriptionItems = new Repository<RecurringSubscriptionItem>(context);
        RecurringOccurrences = new Repository<RecurringOccurrence>(context);
    }

    public IFlowerRepository Flowers { get; }
    public IOrderRepository Orders { get; }
    public IPortfolioRepository Portfolio { get; }
    public ITenantRepository Tenants { get; }
    public IRepository<DeliveryZone> DeliveryZones { get; }
    public IRepository<OrderChangeProposal> OrderChangeProposals { get; }
    public IRepository<AuditLog> AuditLogs { get; }
    public IPaymentRepository Payments { get; }
    public IRepository<PaymentEvent> PaymentEvents { get; }
    public IRepository<PlatformBillingSettings> BillingSettings { get; }
    public IRepository<StripeWebhookReceipt> WebhookReceipts { get; }
    public IRepository<LegalDocumentVersion> LegalDocumentVersions { get; }
    public IRepository<FloristInvoicingSettings> InvoicingSettings { get; }
    public IRepository<SalesDocument> SalesDocuments { get; }
    public IRepository<FloristInvite> FloristInvites { get; }
    public IRepository<FloristTenantMembership> FloristTenantMemberships { get; }
    public IRepository<DeliveryCapacityPolicy> CapacityPolicies { get; }
    public IRepository<DeliveryCapacityWindow> CapacityWindows { get; }
    public IRepository<DeliveryCapacityException> CapacityExceptions { get; }
    public IRepository<DeliveryCapacityHold> CapacityHolds { get; }
    public IRepository<StockMovement> StockMovements { get; }
    public IRepository<Coupon> Coupons { get; }
    public IRepository<CouponTarget> CouponTargets { get; }
    public IRepository<CouponRedemption> CouponRedemptions { get; }
    public IRepository<SeasonalCampaign> Campaigns { get; }
    public IRepository<CampaignFlower> CampaignFlowers { get; }
    public IRepository<OrderReview> Reviews { get; }
    public IRepository<FloristPanelNotification> FloristPanelNotifications { get; }
    public IRepository<RecurringSubscription> RecurringSubscriptions { get; }
    public IRepository<RecurringSubscriptionItem> RecurringSubscriptionItems { get; }
    public IRepository<RecurringOccurrence> RecurringOccurrences { get; }

    public void Add<T>(T entity) where T : class => _context.Set<T>().Add(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _context.Database.BeginTransactionAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational() || _context.Database.CurrentTransaction is not null)
        {
            await action(cancellationToken);
            return;
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });
    }

    public async Task LockCouponAsync(Guid tenantId, Guid couponId, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
            return;
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("O cupão só pode ser reservado dentro de uma transação.");

        var key = CouponLockKey(tenantId, couponId);
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    private static long CouponLockKey(Guid tenantId, Guid couponId)
    {
        Span<byte> bytes = stackalloc byte[16];
        couponId.TryWriteBytes(bytes);
        var left = BitConverter.ToInt64(bytes);
        tenantId.TryWriteBytes(bytes);
        return left ^ BitConverter.ToInt64(bytes) ^ unchecked((long)0xC0FFEE11);
    }
}
