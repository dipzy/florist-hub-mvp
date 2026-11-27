using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Auth;
using FloristHub.Services.DTOs.Legal;
using FloristHub.Services.DTOs.Orders;
using FloristHub.Services.DTOs.Recurring;
using FloristHub.Services.Interfaces.Email;
using FloristHub.Services.Interfaces.Push;
using FloristHub.Services.Orders;
using FloristHub.Services.Recurring;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services;

public interface IRecurringSubscriptionService
{
    Task<RecurringSubscriptionDto> CreateAsync(CreateRecurringSubscriptionRequest request, Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<RecurringSubscriptionDto>> ListForCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<RecurringSubscriptionDto> GetAsync(Guid id, Guid userId, string role, CancellationToken ct = default);
    Task<RecurringSubscriptionDto> PauseAsync(Guid id, Guid customerId, CancellationToken ct = default);
    Task<RecurringSubscriptionDto> ResumeAsync(Guid id, Guid customerId, CancellationToken ct = default);
    Task<RecurringSubscriptionDto> CancelAsync(Guid id, Guid userId, string role, CancellationToken ct = default);
    Task<RecurringSubscriptionDto> SkipNextAsync(Guid id, Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<RecurringSubscriptionDto>> ListForFloristAsync(RecurringSubscriptionStatus? status, CancellationToken ct = default);
    Task<RecurringSubscriptionDto> AcceptAsync(Guid id, Guid floristUserId, FloristRecurringActionRequest request, CancellationToken ct = default);
    Task<RecurringSubscriptionDto> RejectAsync(Guid id, Guid floristUserId, FloristRecurringActionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<FloristUpcomingRecurringDeliveryDto>> ListUpcomingDeliveriesAsync(int days, CancellationToken ct = default);
}

public class RecurringSubscriptionService : IRecurringSubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;
    private readonly ILegalComplianceService _legal;
    private readonly IRecurringEmailNotifications _emails;
    private readonly IRecurringPushNotifications _push;
    private readonly UserManager<ApplicationUser> _users;

    public RecurringSubscriptionService(
        IUnitOfWork unitOfWork,
        ITenantContext tenant,
        IAuditLogger audit,
        ILegalComplianceService legal,
        IRecurringEmailNotifications emails,
        IRecurringPushNotifications push,
        UserManager<ApplicationUser> users)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
        _legal = legal;
        _emails = emails;
        _push = push;
        _users = users;
    }

    public async Task<RecurringSubscriptionDto> CreateAsync(CreateRecurringSubscriptionRequest request, Guid customerId, CancellationToken ct = default)
    {
        RecurringSubscriptionValidation.EnsureValid(request);
        var tenantId = RequireTenant();
        var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsForUpdateAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Loja não encontrada.");

        var orderProbe = RecurringSubscriptionValidation.ToOrderRequest(request);
        TenantOrderRules.EnsureCanAcceptOrder(tenant, orderProbe);

        var currentLegal = await _legal.GetCurrentVersionsAsync(ct);
        _legal.EnsureCheckoutConsent(
            new LegalConsentRequest(request.AcceptTermsAndPrivacy, request.TermsVersion ?? "", request.PrivacyVersion ?? ""),
            currentLegal);

        var subscription = new RecurringSubscription
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Frequency = request.Frequency,
            AnchorDeliveryAt = request.AnchorDeliveryAt,
            DeliveryType = request.DeliveryType,
            DeliveryAddress = request.DeliveryAddress?.Trim(),
            DeliveryZoneId = request.DeliveryZoneId,
            BaseType = request.BaseType,
            ArrangementShape = request.ArrangementShape,
            ArrangementVariationSeed = request.ArrangementVariationSeed,
            ArrangementPreviewUrl = request.ArrangementPreviewUrl,
            CustomerNotes = request.CustomerNotes?.Trim(),
            IsFloristChoice = request.IsFloristChoice,
            FloristChoiceBudget = request.FloristChoiceBudget,
            Status = RecurringSubscriptionStatus.PendingFloristApproval,
            AcceptedTermsVersion = currentLegal.TermsVersion,
            AcceptedPrivacyVersion = currentLegal.PrivacyVersion,
            LegalConsentRecordedAt = DateTimeOffset.UtcNow
        };

        foreach (var item in request.Items)
        {
            subscription.Items.Add(new RecurringSubscriptionItem
            {
                TenantId = tenantId,
                FlowerId = item.FlowerId,
                Name = item.Name.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        await _unitOfWork.RecurringSubscriptions.AddAsync(subscription, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("RecurringSubscriptionCreated", customerId, tenantId, nameof(RecurringSubscription), subscription.Id, null, ct);

        var loaded = await LoadAsync(subscription.Id, ct);
        var customer = loaded.Customer ?? await _users.FindByIdAsync(customerId.ToString())
            ?? throw new InvalidOperationException("Cliente não encontrado.");
        _emails.SubscriptionRequested(loaded, tenant, customer);
        _push.SubscriptionRequested(loaded, tenant);
        return Map(loaded, tenant);
    }

    public async Task<IReadOnlyList<RecurringSubscriptionDto>> ListForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        RequireTenant();
        var tenantId = RequireTenant();
        var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsReadOnlyAsync(tenantId, ct);
        var list = await _unitOfWork.RecurringSubscriptions.Query()
            .Include(s => s.Items)
            .Include(s => s.Customer)
            .Include(s => s.DeliveryZone)
            .Include(s => s.Occurrences)
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
        return list.Select(s => Map(s, tenant)).ToList();
    }

    public async Task<RecurringSubscriptionDto> GetAsync(Guid id, Guid userId, string role, CancellationToken ct = default)
    {
        var subscription = await LoadAsync(id, ct);
        EnsureCanAccess(subscription, userId, role);
        var tenant = await _unitOfWork.Tenants.GetByIdReadOnlyAsync(subscription.TenantId, ct);
        return Map(subscription, tenant);
    }

    public async Task<RecurringSubscriptionDto> PauseAsync(Guid id, Guid customerId, CancellationToken ct = default)
    {
        var subscription = await LoadForUpdateAsync(id, ct);
        if (subscription.CustomerId != customerId)
            throw new UnauthorizedAccessException();
        if (subscription.Status != RecurringSubscriptionStatus.Active)
            throw new InvalidOperationException("Só pode pausar recorrências ativas.");

        subscription.Status = RecurringSubscriptionStatus.Paused;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        var tenant = await RequireTenantEntityAsync(subscription.TenantId, ct);
        var customer = subscription.Customer!;
        _emails.SubscriptionPaused(subscription, tenant, customer);
        _push.SubscriptionPaused(subscription, tenant, customer);
        return Map(await LoadAsync(id, ct), tenant);
    }

    public async Task<RecurringSubscriptionDto> ResumeAsync(Guid id, Guid customerId, CancellationToken ct = default)
    {
        var subscription = await LoadForUpdateAsync(id, ct);
        if (subscription.CustomerId != customerId)
            throw new UnauthorizedAccessException();
        if (subscription.Status != RecurringSubscriptionStatus.Paused)
            throw new InvalidOperationException("A recorrência não está em pausa.");

        subscription.Status = RecurringSubscriptionStatus.Active;
        subscription.NextDeliveryAt ??= ResolveInitialNextDelivery(subscription);
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        var tenant = await RequireTenantEntityAsync(subscription.TenantId, ct);
        var customer = subscription.Customer!;
        _emails.SubscriptionResumed(subscription, tenant, customer);
        _push.SubscriptionResumed(subscription, tenant, customer);
        return Map(await LoadAsync(id, ct), tenant);
    }

    public async Task<RecurringSubscriptionDto> CancelAsync(Guid id, Guid userId, string role, CancellationToken ct = default)
    {
        var subscription = await LoadForUpdateAsync(id, ct);
        EnsureCanAccess(subscription, userId, role);
        if (subscription.Status is RecurringSubscriptionStatus.Cancelled or RecurringSubscriptionStatus.Rejected)
            throw new InvalidOperationException("A recorrência já está terminada.");

        subscription.Status = RecurringSubscriptionStatus.Cancelled;
        subscription.NextDeliveryAt = null;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        var tenant = await RequireTenantEntityAsync(subscription.TenantId, ct);
        var customer = subscription.Customer!;
        _emails.SubscriptionCancelled(subscription, tenant, customer);
        _push.SubscriptionCancelled(subscription, tenant, customer);
        return Map(await LoadAsync(id, ct), tenant);
    }

    public async Task<RecurringSubscriptionDto> SkipNextAsync(Guid id, Guid customerId, CancellationToken ct = default)
    {
        var subscription = await LoadForUpdateAsync(id, ct);
        if (subscription.CustomerId != customerId)
            throw new UnauthorizedAccessException();
        if (subscription.Status != RecurringSubscriptionStatus.Active || subscription.NextDeliveryAt is not { } deliveryAt)
            throw new InvalidOperationException("Não há próxima entrega para saltar.");

        var idempotencyKey = RecurringScheduleCalculator.BuildIdempotencyKey(subscription.Id, deliveryAt);
        var existing = await _unitOfWork.RecurringOccurrences.Query()
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, ct);
        if (existing is null)
        {
            _unitOfWork.Add(new RecurringOccurrence
            {
                TenantId = subscription.TenantId,
                RecurringSubscriptionId = subscription.Id,
                ScheduledDeliveryAt = deliveryAt,
                Status = RecurringOccurrenceStatus.Skipped,
                IdempotencyKey = idempotencyKey
            });
        }
        else
        {
            existing.Status = RecurringOccurrenceStatus.Skipped;
        }

        subscription.NextDeliveryAt = RecurringScheduleCalculator.AdvanceFrom(deliveryAt, subscription.Frequency);
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        var tenant = await RequireTenantEntityAsync(subscription.TenantId, ct);
        var customer = subscription.Customer!;
        _emails.DeliverySkipped(subscription, tenant, customer, deliveryAt);
        _push.DeliverySkipped(subscription, tenant, customer);
        return Map(await LoadAsync(id, ct), tenant);
    }

    public async Task<IReadOnlyList<RecurringSubscriptionDto>> ListForFloristAsync(RecurringSubscriptionStatus? status, CancellationToken ct = default)
    {
        RequireTenant();
        IQueryable<RecurringSubscription> query = _unitOfWork.RecurringSubscriptions.Query()
            .Include(s => s.Items)
            .Include(s => s.Customer)
            .Include(s => s.DeliveryZone)
            .Include(s => s.Occurrences);
        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);
        var tenantId = RequireTenant();
        var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsReadOnlyAsync(tenantId, ct);
        var list = await query.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
        return list.Select(s => Map(s, tenant)).ToList();
    }

    public async Task<RecurringSubscriptionDto> AcceptAsync(Guid id, Guid floristUserId, FloristRecurringActionRequest request, CancellationToken ct = default)
    {
        var subscription = await LoadForUpdateAsync(id, ct);
        if (subscription.Status != RecurringSubscriptionStatus.PendingFloristApproval)
            throw new InvalidOperationException("Esta recorrência já foi respondida.");

        subscription.Status = RecurringSubscriptionStatus.Active;
        subscription.FloristResponseNotes = request.Notes?.Trim();
        subscription.NextDeliveryAt = ResolveInitialNextDelivery(subscription);
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("RecurringSubscriptionAccepted", floristUserId, subscription.TenantId, nameof(RecurringSubscription), subscription.Id, null, ct);

        var tenant = await RequireTenantEntityAsync(subscription.TenantId, ct);
        var customer = subscription.Customer!;
        _emails.SubscriptionAccepted(subscription, tenant, customer);
        _push.SubscriptionAccepted(subscription, tenant, customer);
        return Map(await LoadAsync(id, ct), tenant);
    }

    public async Task<RecurringSubscriptionDto> RejectAsync(Guid id, Guid floristUserId, FloristRecurringActionRequest request, CancellationToken ct = default)
    {
        var subscription = await LoadForUpdateAsync(id, ct);
        if (subscription.Status != RecurringSubscriptionStatus.PendingFloristApproval)
            throw new InvalidOperationException("Esta recorrência já foi respondida.");

        subscription.Status = RecurringSubscriptionStatus.Rejected;
        subscription.FloristResponseNotes = request.Notes?.Trim();
        subscription.NextDeliveryAt = null;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("RecurringSubscriptionRejected", floristUserId, subscription.TenantId, nameof(RecurringSubscription), subscription.Id, null, ct);

        var tenant = await RequireTenantEntityAsync(subscription.TenantId, ct);
        var customer = subscription.Customer!;
        _emails.SubscriptionRejected(subscription, tenant, customer);
        _push.SubscriptionRejected(subscription, tenant, customer);
        return Map(await LoadAsync(id, ct), tenant);
    }

    public async Task<IReadOnlyList<FloristUpcomingRecurringDeliveryDto>> ListUpcomingDeliveriesAsync(int days, CancellationToken ct = default)
    {
        RequireTenant();
        days = Math.Clamp(days, 1, 90);
        var until = DateTimeOffset.UtcNow.AddDays(days);
        var list = await _unitOfWork.RecurringSubscriptions.Query()
            .Include(s => s.Customer)
            .Where(s => s.Status == RecurringSubscriptionStatus.Active
                        && s.NextDeliveryAt != null
                        && s.NextDeliveryAt <= until)
            .OrderBy(s => s.NextDeliveryAt)
            .ToListAsync(ct);

        return list.Select(s => new FloristUpcomingRecurringDeliveryDto(
            s.Id,
            s.CustomerId,
            s.Customer?.FullName,
            s.NextDeliveryAt!.Value,
            s.DeliveryType,
            s.DeliveryAddress,
            s.IsFloristChoice)).ToList();
    }

    private static DateTimeOffset ResolveInitialNextDelivery(RecurringSubscription subscription)
    {
        var now = DateTimeOffset.UtcNow;
        if (subscription.AnchorDeliveryAt > now)
            return subscription.AnchorDeliveryAt;
        return RecurringScheduleCalculator.ComputeNextDelivery(
            subscription.AnchorDeliveryAt,
            subscription.Frequency,
            now);
    }

    private async Task<RecurringSubscription> LoadAsync(Guid id, CancellationToken ct) =>
        await _unitOfWork.RecurringSubscriptions.Query()
            .Include(s => s.Items)
            .Include(s => s.Customer)
            .Include(s => s.DeliveryZone)
            .Include(s => s.Occurrences)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new KeyNotFoundException("Recorrência não encontrada.");

    private async Task<RecurringSubscription> LoadForUpdateAsync(Guid id, CancellationToken ct) =>
        await _unitOfWork.RecurringSubscriptions.Query()
            .Include(s => s.Items)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new KeyNotFoundException("Recorrência não encontrada.");

    private async Task<Tenant> RequireTenantEntityAsync(Guid tenantId, CancellationToken ct) =>
        await _unitOfWork.Tenants.GetByIdReadOnlyAsync(tenantId, ct)
        ?? throw new InvalidOperationException("Loja não encontrada.");

    private void EnsureCanAccess(RecurringSubscription subscription, Guid userId, string role)
    {
        if (role == DAL.Constants.Roles.Florist)
        {
            if (_tenant.TenantId == subscription.TenantId)
                return;
            throw new UnauthorizedAccessException();
        }

        if (role == DAL.Constants.Roles.Customer && subscription.CustomerId == userId)
            return;
        throw new UnauthorizedAccessException();
    }

    private Guid RequireTenant()
    {
        if (!_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Tenant não resolvido.");
        return _tenant.TenantId.Value;
    }

    private static RecurringSubscriptionDto Map(RecurringSubscription subscription, Tenant? tenant)
    {
        var items = subscription.Items
            .Select(i => new RecurringSubscriptionItemDto(i.Id, i.FlowerId, i.Name, i.Quantity, i.UnitPrice))
            .ToList();
        var itemsTotal = items.Sum(i => i.UnitPrice * i.Quantity);
        if (subscription.IsFloristChoice && subscription.FloristChoiceBudget is { } budget)
            itemsTotal = budget;

        var deliveryFee = 0m;
        if (tenant is not null)
        {
            var probe = new CreateOrderRequest(
                subscription.BaseType,
                subscription.DeliveryType,
                subscription.DeliveryAddress,
                subscription.NextDeliveryAt,
                subscription.CustomerNotes,
                null,
                subscription.DeliveryZoneId,
                items.Select(i => new OrderItemRequest(i.FlowerId, i.Name, i.Quantity, i.UnitPrice)).ToList(),
                subscription.ArrangementShape,
                subscription.ArrangementVariationSeed,
                subscription.ArrangementPreviewUrl);
            var zone = subscription.DeliveryZoneId is null
                ? null
                : tenant.DeliveryZones.FirstOrDefault(z => z.Id == subscription.DeliveryZoneId);
            deliveryFee = TenantOrderRules.ResolveDeliveryFee(tenant, probe, zone);
        }

        var occurrences = subscription.Occurrences
            .OrderByDescending(o => o.ScheduledDeliveryAt)
            .Take(6)
            .Select(o => new RecurringOccurrenceDto(o.Id, o.ScheduledDeliveryAt, o.Status, o.GeneratedOrderId))
            .ToList();

        return new RecurringSubscriptionDto(
            subscription.Id,
            subscription.Status,
            subscription.Frequency,
            subscription.AnchorDeliveryAt,
            subscription.NextDeliveryAt,
            subscription.DeliveryType,
            subscription.DeliveryAddress,
            subscription.DeliveryZoneId,
            subscription.DeliveryZone?.Name,
            subscription.BaseType,
            subscription.IsFloristChoice,
            subscription.FloristChoiceBudget,
            itemsTotal,
            deliveryFee,
            subscription.CustomerNotes,
            subscription.FloristResponseNotes,
            subscription.Customer?.FullName,
            subscription.CreatedAt,
            items,
            occurrences);
    }
}
