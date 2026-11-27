using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Orders;
using FloristHub.Services.Interfaces.Email;
using FloristHub.Services.Interfaces.Push;
using FloristHub.Services.Interfaces.Visualization;
using FloristHub.Services.Invoicing;
using FloristHub.Services.Capacity;
using FloristHub.Services.Marketing;
using FloristHub.Services.Notifications;
using FloristHub.Services.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Recurring;

public interface IRecurringOrderGenerationService
{
    Task<int> ProcessDueAsync(CancellationToken ct = default);
}

public sealed class RecurringOrderGenerationService : IRecurringOrderGenerationService
{
    private readonly ApplicationDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RecurringOrderOptions _options;
    private readonly ILogger<RecurringOrderGenerationService> _logger;

    public RecurringOrderGenerationService(
        ApplicationDbContext db,
        IServiceScopeFactory scopeFactory,
        IOptions<RecurringOrderOptions> options,
        ILogger<RecurringOrderGenerationService> logger)
    {
        _db = db;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ProcessDueAsync(CancellationToken ct = default)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var leaseUntil = utcNow.AddSeconds(_options.GenerationLeaseSeconds);
        var due = await _db.RecurringSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Status == RecurringSubscriptionStatus.Active
                        && s.NextDeliveryAt != null
                        && (s.GenerationLeaseUntil == null || s.GenerationLeaseUntil < utcNow))
            .OrderBy(s => s.NextDeliveryAt)
            .Take(50)
            .ToListAsync(ct);

        var generated = 0;
        foreach (var subscription in due)
        {
            if (subscription.NextDeliveryAt is not { } deliveryAt)
                continue;
            if (!RecurringScheduleCalculator.ShouldGenerateOrder(deliveryAt, _options.GenerationAdvanceDays, utcNow))
                continue;

            subscription.GenerationLeaseUntil = leaseUntil;
            await _db.SaveChangesAsync(ct);

            try
            {
                if (await TryGenerateAsync(subscription.Id, ct))
                    generated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao gerar encomenda recorrente {SubscriptionId}", subscription.Id);
            }
            finally
            {
                var tracked = await _db.RecurringSubscriptions.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.Id == subscription.Id, ct);
                if (tracked is not null)
                {
                    tracked.GenerationLeaseUntil = null;
                    await _db.SaveChangesAsync(ct);
                }
            }
        }

        return generated;
    }

    private async Task<bool> TryGenerateAsync(Guid subscriptionId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var tenantContext = (TenantContext)scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var arrangement3D = scope.ServiceProvider.GetRequiredService<IArrangement3DService>();
        var emails = scope.ServiceProvider.GetRequiredService<IOrderEmailNotifications>();
        var panel = scope.ServiceProvider.GetRequiredService<IFloristPanelNotificationService>();
        var recurringEmails = scope.ServiceProvider.GetRequiredService<IRecurringEmailNotifications>();
        var push = scope.ServiceProvider.GetRequiredService<IOrderPushNotifications>();
        var recurringPush = scope.ServiceProvider.GetRequiredService<IRecurringPushNotifications>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
        var capacity = scope.ServiceProvider.GetRequiredService<IDeliveryCapacityService>();
        var capacityGate = scope.ServiceProvider.GetRequiredService<CapacityGate>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var owner = await db.RecurringSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Id == subscriptionId)
            .Select(s => new { s.TenantId })
            .FirstOrDefaultAsync(ct);
        if (owner is null)
            return false;

        tenantContext.SetTenant(owner.TenantId);

        var subscription = await unitOfWork.RecurringSubscriptions.Query()
            .Include(s => s.Items)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);
        if (subscription is null
            || subscription.Status != RecurringSubscriptionStatus.Active
            || subscription.NextDeliveryAt is not { } deliveryAt)
            return false;

        var idempotencyKey = RecurringScheduleCalculator.BuildIdempotencyKey(subscription.Id, deliveryAt);
        var existingOccurrence = await unitOfWork.RecurringOccurrences.Query()
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, ct);

        if (existingOccurrence is { Status: RecurringOccurrenceStatus.OrderGenerated or RecurringOccurrenceStatus.Skipped })
        {
            await AdvanceNextDeliveryAsync(subscription, deliveryAt, unitOfWork, ct);
            return false;
        }

        var strategy = db.Database.CreateExecutionStrategy();
        var serviceDate = CapacityRules.DateOf(deliveryAt);
        SemaphoreSlim? slot = null;
        if (serviceDate is not null)
        {
            slot = capacityGate.For(subscription.TenantId, serviceDate.Value);
            await slot.WaitAsync(ct);
        }

        try
        {
        return await strategy.ExecuteAsync(async () =>
        {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var occurrence = existingOccurrence ?? new RecurringOccurrence
            {
                TenantId = subscription.TenantId,
                RecurringSubscriptionId = subscription.Id,
                ScheduledDeliveryAt = deliveryAt,
                Status = RecurringOccurrenceStatus.Scheduled,
                IdempotencyKey = idempotencyKey
            };
            if (existingOccurrence is null)
                unitOfWork.Add(occurrence);

            var tenant = await unitOfWork.Tenants.GetByIdWithSettingsForUpdateAsync(subscription.TenantId, ct)
                ?? throw new InvalidOperationException("Loja não encontrada.");

            var orderRequest = BuildOrderRequest(subscription, deliveryAt);
            TenantOrderRules.EnsureCanAcceptOrder(tenant, orderRequest);

            DeliveryZone? zone = null;
            if (subscription.DeliveryZoneId is Guid zoneId)
                zone = tenant.DeliveryZones.FirstOrDefault(z => z.Id == zoneId && z.IsActive);

            var deliveryFee = TenantOrderRules.ResolveDeliveryFee(tenant, orderRequest, zone);

            var order = new Order
            {
                TenantId = subscription.TenantId,
                CustomerId = subscription.CustomerId,
                Status = OrderStatus.PendingConfirmation,
                BaseType = subscription.BaseType,
                ArrangementShape = subscription.ArrangementShape,
                ArrangementVariationSeed = subscription.ArrangementVariationSeed,
                ArrangementPreviewUrl = subscription.ArrangementPreviewUrl,
                DeliveryType = subscription.DeliveryType,
                DeliveryAddress = subscription.DeliveryAddress,
                RequestedDeliveryAt = deliveryAt,
                CustomerNotes = BuildRecurringNotes(subscription),
                DeliveryZoneId = zone?.Id,
                DeliveryFee = deliveryFee,
                EstimatedTotal = 0,
                DiscountAmount = 0,
                TrackingAccessToken = FloristHub.Services.Orders.OrderTrackingToken.Create(),
                RecurringSubscriptionId = subscription.Id,
                RecurringOccurrenceId = occurrence.Id,
                AcceptedTermsVersion = subscription.AcceptedTermsVersion,
                AcceptedPrivacyVersion = subscription.AcceptedPrivacyVersion,
                LegalConsentRecordedAt = subscription.LegalConsentRecordedAt
            };

            foreach (var item in orderRequest.Items)
            {
                order.Items.Add(new OrderItem
                {
                    TenantId = subscription.TenantId,
                    FlowerId = item.FlowerId,
                    Name = item.Name.Trim(),
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    VatRate = VatRates.StandardMainland
                });
            }

            await OrderPricing.ApplySpecialPricesAsync(unitOfWork, order, ct);
            await OrderPricing.CopyCategoriesAsync(unitOfWork, order, ct);
            OrderPricing.RefreshPayable(order, 0);

            if (db.Database.IsRelational() && serviceDate is not null)
            {
                var key = CapacityRules.AdvisoryKey(subscription.TenantId, serviceDate.Value);
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", ct);
            }

            try
            {
                await capacity.ApplyReserveAsync(order, tenant, ct);
            }
            catch (InvalidOperationException ex) when (IsCapacityRefusal(ex))
            {
                occurrence.Status = RecurringOccurrenceStatus.Skipped;
                subscription.NextDeliveryAt = RecurringScheduleCalculator.AdvanceFrom(deliveryAt, subscription.Frequency);
                subscription.UpdatedAt = DateTimeOffset.UtcNow;
                await unitOfWork.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                var blockedCustomer = subscription.Customer ?? await _db.Users.AsNoTracking()
                    .FirstAsync(u => u.Id == subscription.CustomerId, ct);
                recurringEmails.CapacityUnavailable(subscription, tenant, blockedCustomer, deliveryAt);
                recurringPush.CapacityUnavailable(subscription, tenant, blockedCustomer, deliveryAt);
                await audit.LogAsync(
                    "RecurringCapacityUnavailable",
                    subscription.CustomerId,
                    subscription.TenantId,
                    nameof(RecurringSubscription),
                    subscription.Id,
                    ex.Message,
                    ct);
                return false;
            }

            order.StatusHistory.Add(new OrderStatusHistory
            {
                TenantId = subscription.TenantId,
                FromStatus = OrderStatus.PendingConfirmation,
                ToStatus = OrderStatus.PendingConfirmation,
                ChangedByUserId = subscription.CustomerId,
                Notes = "Encomenda gerada automaticamente a partir de recorrência."
            });

            order.Arrangement3DPreviewToken = await arrangement3D.GeneratePreviewTokenAsync(
                order.Id,
                order.ArrangementVariationSeed,
                order.ArrangementShape,
                order.BaseType,
                ct);

            await unitOfWork.Orders.AddAsync(order, ct);
            await unitOfWork.SaveChangesAsync(ct);

            occurrence.GeneratedOrderId = order.Id;
            occurrence.Status = RecurringOccurrenceStatus.OrderGenerated;
            subscription.NextDeliveryAt = RecurringScheduleCalculator.AdvanceFrom(deliveryAt, subscription.Frequency);
            subscription.UpdatedAt = DateTimeOffset.UtcNow;

            await unitOfWork.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var loaded = await unitOfWork.Orders.GetByIdWithDetailsAsync(order.Id, ct) ?? order;
            emails.OrderCreated(loaded, tenant);
            push.OrderCreated(loaded, tenant);
            await panel.RecordNewOrderAsync(subscription.TenantId, order.Id, ct);
            var customer = subscription.Customer ?? await _db.Users.AsNoTracking()
                .FirstAsync(u => u.Id == subscription.CustomerId, ct);
            recurringEmails.OrderGenerated(subscription, tenant, customer, loaded);
            recurringPush.OrderGenerated(subscription, tenant, customer, loaded);
            await audit.LogAsync(
                "RecurringOrderGenerated",
                subscription.CustomerId,
                subscription.TenantId,
                nameof(Order),
                order.Id,
                $"Subscription={subscription.Id}",
                ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await tx.RollbackAsync(ct);
            _logger.LogInformation("Geração idempotente ignorada para {SubscriptionId}", subscriptionId);
            return false;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
        });
        }
        finally
        {
            slot?.Release();
        }
    }

    private static async Task AdvanceNextDeliveryAsync(
        RecurringSubscription subscription,
        DateTimeOffset deliveryAt,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        if (subscription.NextDeliveryAt <= deliveryAt)
        {
            subscription.NextDeliveryAt = RecurringScheduleCalculator.AdvanceFrom(deliveryAt, subscription.Frequency);
            subscription.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    private static CreateOrderRequest BuildOrderRequest(RecurringSubscription subscription, DateTimeOffset deliveryAt)
    {
        var items = subscription.IsFloristChoice
            ? new List<OrderItemRequest>
            {
                new(null, "Escolha da florista", 1, subscription.FloristChoiceBudget ?? 0)
            }
            : subscription.Items.Select(i => new OrderItemRequest(i.FlowerId, i.Name, i.Quantity, i.UnitPrice)).ToList();

        return new CreateOrderRequest(
            subscription.BaseType,
            subscription.DeliveryType,
            subscription.DeliveryAddress,
            deliveryAt,
            subscription.CustomerNotes,
            null,
            subscription.DeliveryZoneId,
            items,
            subscription.ArrangementShape,
            subscription.ArrangementVariationSeed,
            subscription.ArrangementPreviewUrl);
    }

    private static string BuildRecurringNotes(RecurringSubscription subscription) =>
        string.IsNullOrWhiteSpace(subscription.CustomerNotes)
            ? $"Recorrência #{subscription.Id.ToString()[..8]}"
            : $"{subscription.CustomerNotes} (recorrência)";

    private static bool IsCapacityRefusal(InvalidOperationException ex) =>
        ex.Message.Contains("vaga", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("indisponível", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("janela", StringComparison.OrdinalIgnoreCase);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;
}
