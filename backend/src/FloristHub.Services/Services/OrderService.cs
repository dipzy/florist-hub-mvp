using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Orders;
using FloristHub.Services.Interfaces.Email;
using FloristHub.Services.Interfaces.Push;
using FloristHub.Services.DTOs.Payments;
using FloristHub.Services.Interfaces.Visualization;
using FloristHub.Services.Payments;
using FloristHub.Services.DTOs.Common;
using FloristHub.Services.Orders;
using FloristHub.Services.DTOs.Legal;
using FloristHub.Services.Capacity;
using FloristHub.Services.Invoicing;
using FloristHub.Services.Stock;
using FloristHub.Services.Marketing;
using FloristHub.Services.Notifications;
using FloristHub.Services.Orders;
using FloristHub.Services.Validation;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request, Guid customerId, string? clientSubmissionId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> ListForCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> ListForFloristAsync(OrderStatus? status, CancellationToken ct = default);
    Task<PagedResult<OrderDto>> ListForFloristPagedAsync(
        OrderStatus? status,
        DeliveryType? deliveryType,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? customerSearch,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<OrderDto> GetByIdAsync(Guid id, Guid userId, string role, CancellationToken ct = default);
    Task<OrderDto> ConfirmAsync(Guid id, Guid floristUserId, FloristOrderActionRequest request, CancellationToken ct = default);
    Task<OrderDto> RejectAsync(Guid id, Guid floristUserId, FloristOrderActionRequest request, CancellationToken ct = default);
    Task<OrderDto> ProposeChangesAsync(Guid id, Guid floristUserId, CreateOrderChangeProposalRequest request, CancellationToken ct = default);
    Task<OrderDto> AcceptChangeProposalAsync(Guid orderId, Guid proposalId, Guid customerId, RespondToOrderChangeProposalRequest request, CancellationToken ct = default);
    Task<OrderDto> RejectChangeProposalAsync(Guid orderId, Guid proposalId, Guid customerId, RespondToOrderChangeProposalRequest request, CancellationToken ct = default);
    Task<OrderDto> UpdateStatusAsync(Guid id, Guid userId, UpdateOrderStatusRequest request, CancellationToken ct = default);
    Task<OrderDto> CancelAsync(Guid id, Guid customerId, CancelOrderRequest request, CancellationToken ct = default);
    Task<OrderDto> ApplyCouponAsync(Guid id, Guid customerId, string code, CancellationToken ct = default);
    Task<OrderDto> RemoveCouponAsync(Guid id, Guid customerId, CancellationToken ct = default);
    Task<CheckoutSessionDto> InitiatePaymentAsync(
        Guid id,
        Guid customerId,
        string? successUrl,
        string? cancelUrl,
        LegalConsentRequest? legalConsent,
        string? customerTaxId = null,
        CancellationToken ct = default);
}

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;
    private readonly IPaymentService _payments;
    private readonly IArrangement3DService _arrangement3D;
    private readonly IOrderEmailNotifications _emails;
    private readonly IOrderPushNotifications _push;
    private readonly ILegalComplianceService _legal;
    private readonly IDeliveryCapacityService? _capacity;
    private readonly IStockService? _stock;
    private readonly IFloristPanelNotificationService _panel;

    public OrderService(
        IUnitOfWork unitOfWork,
        ITenantContext tenant,
        IAuditLogger audit,
        IPaymentService payments,
        IArrangement3DService arrangement3D,
        IOrderEmailNotifications emails,
        IOrderPushNotifications push,
        ILegalComplianceService legal,
        IFloristPanelNotificationService panel,
        IDeliveryCapacityService? capacity = null,
        IStockService? stock = null)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
        _payments = payments;
        _arrangement3D = arrangement3D;
        _emails = emails;
        _push = push;
        _legal = legal;
        _panel = panel;
        _capacity = capacity;
        _stock = stock;
    }

    public async Task<OrderDto> CreateAsync(
        CreateOrderRequest request,
        Guid customerId,
        string? clientSubmissionId,
        CancellationToken ct = default)
    {
        OrderValidation.EnsureValid(request);
        var tenantId = RequireTenant();
        var submissionKey = string.IsNullOrWhiteSpace(clientSubmissionId) ? null : clientSubmissionId.Trim();
        if (submissionKey is not null)
        {
            var dup = await _unitOfWork.Orders.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    o => o.TenantId == tenantId && o.CustomerId == customerId && o.ClientSubmissionId == submissionKey,
                    ct);
            if (dup is not null)
                return Map(await LoadOrderAsync(dup.Id, ct));
        }
        var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsForUpdateAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Loja não encontrada.");

        TenantOrderRules.EnsureCanAcceptOrder(tenant, request);
        await ValidateOrderReferencesAsync(tenantId, request, ct);

        DeliveryZone? zone = null;
        if (request.DeliveryZoneId is Guid zoneId)
        {
            zone = tenant.DeliveryZones.FirstOrDefault(z => z.Id == zoneId && z.IsActive)
                ?? throw new InvalidOperationException("Zona de entrega inválida ou inativa.");
        }

        var deliveryFee = TenantOrderRules.ResolveDeliveryFee(tenant, request, zone);
        var defaultVat = await DefaultVatRateAsync(ct);
        var vatByFlower = await FlowerVatRatesAsync(request.Items.Select(i => i.FlowerId), ct);
        var order = new Order
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Status = OrderStatus.PendingConfirmation,
            BaseType = request.BaseType,
            ArrangementShape = request.ArrangementShape,
            ArrangementVariationSeed = request.ArrangementVariationSeed,
            ArrangementPreviewUrl = string.IsNullOrWhiteSpace(request.ArrangementPreviewUrl)
                ? null
                : request.ArrangementPreviewUrl.Trim(),
            DeliveryType = request.DeliveryType,
            DeliveryAddress = request.DeliveryAddress?.Trim(),
            RequestedDeliveryAt = request.RequestedDeliveryAt,
            CustomerNotes = request.CustomerNotes?.Trim(),
            CustomerTaxId = string.IsNullOrWhiteSpace(request.CustomerTaxId) ? null : PortugueseNif.Normalize(request.CustomerTaxId),
            InspiredByPortfolioArrangementId = request.InspiredByPortfolioArrangementId,
            DeliveryZoneId = zone?.Id,
            DeliveryFee = deliveryFee,
            EstimatedTotal = 0,
            TrackingAccessToken = OrderTrackingToken.Create(),
            ClientSubmissionId = submissionKey,
        };

        foreach (var item in request.Items)
        {
            order.Items.Add(new OrderItem
            {
                TenantId = tenantId,
                FlowerId = item.FlowerId,
                Name = item.Name.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                VatRate = ResolveVat(item.FlowerId, vatByFlower, defaultVat)
            });
        }

        await OrderPricing.ApplySpecialPricesAsync(_unitOfWork, order, ct);
        await OrderPricing.CopyCategoriesAsync(_unitOfWork, order, ct);
        OrderPricing.RefreshPayable(order, 0);

        order.StatusHistory.Add(new OrderStatusHistory
        {
            TenantId = tenantId,
            FromStatus = OrderStatus.PendingConfirmation,
            ToStatus = OrderStatus.PendingConfirmation,
            ChangedByUserId = customerId,
            Notes = request.InspiredByPortfolioArrangementId.HasValue
                ? "Encomenda criada pelo cliente (inspirada no portfolio)."
                : "Encomenda criada pelo cliente."
        });

        order.Arrangement3DPreviewToken = await _arrangement3D.GeneratePreviewTokenAsync(
            order.Id,
            order.ArrangementVariationSeed,
            order.ArrangementShape,
            order.BaseType,
            ct);

        await _legal.RecordOrderConsentAsync(
            order,
            new LegalConsentRequest(request.AcceptTermsAndPrivacy, request.TermsVersion ?? "", request.PrivacyVersion ?? ""),
            customerId,
            ct);

        async Task PersistAsync(CancellationToken inner)
        {
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
                await OrderPricing.ApplyCouponAsync(_unitOfWork, order, request.CouponCode, customerId, inner);
            if (_capacity is not null)
                await _capacity.ApplyReserveAsync(order, tenant, inner);
            await _unitOfWork.Orders.AddAsync(order, inner);
            await _unitOfWork.SaveChangesAsync(inner);
        }

        if (_capacity is null)
            await _unitOfWork.ExecuteInTransactionAsync(PersistAsync, ct);
        else
        {
            var date = CapacityRules.DateOf(order.RequestedDeliveryAt);
            await _capacity.RunLockedAsync(tenantId, date, PersistAsync, ct);
        }
        await _audit.LogAsync("OrderCreated", customerId, tenantId, nameof(Order), order.Id, $"Total={order.EstimatedTotal}", ct);
        var created = await LoadOrderAsync(order.Id, ct);
        try
        {
            _emails.OrderCreated(created, tenant);
            _push.OrderCreated(created, tenant);
            await _panel.RecordNewOrderAsync(tenantId, order.Id, ct);
        }
        catch (Exception ex)
        {
            // A encomenda já foi persistida; não falhar o pedido por notificações secundárias.
            await _audit.LogAsync(
                "OrderCreatedNotificationsFailed",
                customerId,
                tenantId,
                nameof(Order),
                order.Id,
                ex.Message,
                ct);
        }

        return Map(created);
    }

    public async Task<IReadOnlyList<OrderDto>> ListForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var orders = await _unitOfWork.Orders.QueryWithDetails().Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
        return orders.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> ListForFloristAsync(OrderStatus? status, CancellationToken ct = default)
    {
        var query = _unitOfWork.Orders.QueryWithDetails();
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var orders = await query.OrderBy(o => o.Status).ThenBy(o => o.CreatedAt).ToListAsync(ct);
        return orders.Select(Map).ToList();
    }

    public async Task<PagedResult<OrderDto>> ListForFloristPagedAsync(
        OrderStatus? status,
        DeliveryType? deliveryType,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? customerSearch,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (items, total) = await _unitOfWork.Orders.SearchForFloristPagedAsync(
            status, deliveryType, fromDate, toDate, customerSearch, page, pageSize, ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return new PagedResult<OrderDto>(items.Select(Map).ToList(), total, page, pageSize);
    }

    public async Task<OrderDto> GetByIdAsync(Guid id, Guid userId, string role, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(id, ct);
        EnsureCanAccess(order, userId, role);
        return Map(order);
    }

    public async Task<OrderDto> ConfirmAsync(Guid id, Guid floristUserId, FloristOrderActionRequest request, CancellationToken ct = default)
    {
        OrderValidation.EnsureValid(request);
        await TransitionAsync(id, floristUserId, OrderStatus.Confirmed, request.Notes ?? "Confirmada pela florista.", "OrderConfirmed", ct, publishPush: false);
        await NotifyOrderConfirmedAsync(id, ct);
        return Map(await LoadOrderAsync(id, ct));
    }

    public async Task<OrderDto> RejectAsync(Guid id, Guid floristUserId, FloristOrderActionRequest request, CancellationToken ct = default)
    {
        OrderValidation.EnsureRejectReason(request.Notes);
        var dto = await TransitionAsync(id, floristUserId, OrderStatus.Rejected, request.Notes!.Trim(), "OrderRejected", ct);
        await NotifyOrderRejectedAsync(id, request.Notes!.Trim(), ct);
        return dto;
    }

    public async Task<OrderDto> ProposeChangesAsync(Guid id, Guid floristUserId, CreateOrderChangeProposalRequest request, CancellationToken ct = default)
    {
        OrderValidation.EnsureValid(request);

        var order = await _unitOfWork.Orders.GetByIdWithStatusHistoryForUpdateAsync(id, ct)
            ?? throw new KeyNotFoundException("Encomenda não encontrada.");

        if (order.Status != OrderStatus.PendingConfirmation)
            throw new InvalidOperationException("Só é possível propor alterações enquanto a encomenda está pendente.");

        var proposal = new OrderChangeProposal
        {
            TenantId = order.TenantId,
            OrderId = order.Id,
            ProposedByUserId = floristUserId,
            Message = request.Message.Trim(),
            ProposedItemChangesJson = request.ProposedItemChangesJson,
            RevisedTotal = request.RevisedTotal,
            Status = OrderChangeProposalStatus.Pending
        };
        await _unitOfWork.OrderChangeProposals.AddAsync(proposal, ct);
        _unitOfWork.Add(new OrderStatusHistory
        {
            TenantId = order.TenantId,
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = order.Status,
            ChangedByUserId = floristUserId,
            Notes = $"Proposta de alteração: {request.Message}"
        });
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("OrderChangeProposed", floristUserId, order.TenantId, nameof(OrderChangeProposal), proposal.Id, request.Message, ct);
        var loaded = await LoadOrderAsync(id, ct);
        var tenantForEmail = await LoadTenantAsync(order.TenantId, ct);
        _emails.ChangeProposalSent(loaded, tenantForEmail, proposal);
        return Map(loaded);
    }

    public async Task<OrderDto> AcceptChangeProposalAsync(
        Guid orderId,
        Guid proposalId,
        Guid customerId,
        RespondToOrderChangeProposalRequest request,
        CancellationToken ct = default) =>
        await RespondToProposalAsync(orderId, proposalId, customerId, accepted: true, request.Notes, ct);

    public async Task<OrderDto> RejectChangeProposalAsync(
        Guid orderId,
        Guid proposalId,
        Guid customerId,
        RespondToOrderChangeProposalRequest request,
        CancellationToken ct = default) =>
        await RespondToProposalAsync(orderId, proposalId, customerId, accepted: false, request.Notes, ct);

    public async Task<OrderDto> UpdateStatusAsync(Guid id, Guid userId, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(request.NewStatus))
            throw new InvalidOperationException("Estado inválido.");

        return await TransitionAsync(id, userId, request.NewStatus, request.Notes ?? "Atualização de estado.", "OrderStatusChanged", ct);
    }

    public async Task<OrderDto> ApplyCouponAsync(Guid id, Guid customerId, string code, CancellationToken ct = default)
    {
        var order = await LoadTrackedOrderAsync(id, ct);
        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("Encomenda não pertence ao cliente.");
        await _unitOfWork.ExecuteInTransactionAsync(async inner =>
        {
            await OrderPricing.ApplyCouponAsync(_unitOfWork, order, code, customerId, inner);
            order.UpdatedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(inner);
            await _audit.LogAsync("CouponApplied", customerId, order.TenantId, nameof(Order), order.Id, order.CouponCode, inner);
        }, ct);

        return Map(await LoadOrderAsync(order.Id, ct));
    }

    public async Task<OrderDto> RemoveCouponAsync(Guid id, Guid customerId, CancellationToken ct = default)
    {
        var order = await LoadTrackedOrderAsync(id, ct);
        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("Encomenda não pertence ao cliente.");
        await OrderPricing.RemoveCouponAsync(_unitOfWork, order, ct);
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("CouponRemoved", customerId, order.TenantId, nameof(Order), order.Id, null, ct);
        return Map(await LoadOrderAsync(order.Id, ct));
    }

    private async Task<Order> LoadTrackedOrderAsync(Guid id, CancellationToken ct) =>
        await _unitOfWork.Orders.Query()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
        ?? throw new KeyNotFoundException("Encomenda não encontrada.");

    public async Task<OrderDto> CancelAsync(Guid id, Guid customerId, CancelOrderRequest request, CancellationToken ct = default)
    {
        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(id, ct)
            ?? throw new KeyNotFoundException("Encomenda não encontrada.");

        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("Encomenda não pertence ao cliente.");

        OrderStateMachine.EnsureCanTransition(order.Status, OrderStatus.Cancelled);
        await _payments.SettleCustomerCancellationAsync(order.Id, ct);
        await OrderPricing.VoidCouponAsync(_unitOfWork, order.Id, ct);

        return await TransitionAsync(
            id,
            customerId,
            OrderStatus.Cancelled,
            request.Notes?.Trim() ?? "Cancelada pelo cliente.",
            "OrderCancelled",
            ct);
    }

    public async Task<CheckoutSessionDto> InitiatePaymentAsync(
        Guid id,
        Guid customerId,
        string? successUrl,
        string? cancelUrl,
        LegalConsentRequest? legalConsent,
        string? customerTaxId = null,
        CancellationToken ct = default)
    {
        if (legalConsent is null)
            throw new InvalidOperationException("Aceitação dos termos e privacidade é obrigatória para pagar.");

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(id, ct)
            ?? throw new KeyNotFoundException("Encomenda não encontrada.");
        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException();

        await _legal.RecordPaymentConsentAsync(order, legalConsent, customerId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return await _payments.StartCheckoutAsync(id, customerId, successUrl, cancelUrl, customerTaxId, ct);
    }

    private async Task<OrderDto> TransitionAsync(
        Guid id,
        Guid userId,
        OrderStatus to,
        string notes,
        string auditAction,
        CancellationToken ct,
        bool publishPush = true)
    {
        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(id, ct)
            ?? throw new KeyNotFoundException("Encomenda não encontrada.");

        async Task Mutate(CancellationToken inner)
        {
            OrderStateMachine.EnsureCanTransition(order.Status, to);
            if (to == OrderStatus.Cancelled)
                await OrderPricing.VoidCouponAsync(_unitOfWork, order.Id, inner);
            if (_stock is not null && to == OrderStatus.Confirmed)
                await _stock.ApplyConfirmAsync(order, inner);
            if (_capacity is not null && to is OrderStatus.Cancelled or OrderStatus.Rejected)
                await _capacity.ApplyReleaseAsync(order, inner);
            if (_stock is not null && to == OrderStatus.Cancelled)
                await _stock.ApplyRestoreAsync(order, StockMovementReason.OrderCancelled, inner);

            var from = order.Status;
            order.Status = to;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.Add(new OrderStatusHistory
            {
                TenantId = order.TenantId,
                OrderId = order.Id,
                FromStatus = from,
                ToStatus = to,
                ChangedByUserId = userId,
                Notes = notes
            });
            await _unitOfWork.SaveChangesAsync(inner);
            await _audit.LogAsync(auditAction, userId, order.TenantId, nameof(Order), order.Id, $"{from}->{to}", inner);
        }

        if (_stock is not null && to is OrderStatus.Confirmed or OrderStatus.Cancelled)
        {
            if (_capacity is null)
                await _stock.WithFlowerLocksAsync(order, Mutate, ct);
            else
                await _capacity.RunLockedAsync(order.TenantId, order.CapacityServiceDate, inner => _stock.WithFlowerLocksAsync(order, Mutate, inner), ct);
        }
        else if (_capacity is not null)
        {
            await _capacity.RunLockedAsync(order.TenantId, order.CapacityServiceDate, Mutate, ct);
        }
        else
        {
            await Mutate(ct);
        }

        if (_stock is not null)
            await _stock.FlushNoticesAsync(ct);
        await NotifyStatusTransitionAsync(id, to, notes, publishPush, ct);
        return Map(await LoadOrderAsync(id, ct));
    }

    private async Task<OrderDto> RespondToProposalAsync(
        Guid orderId,
        Guid proposalId,
        Guid customerId,
        bool accepted,
        string? notes,
        CancellationToken ct)
    {
        var order = await _unitOfWork.Orders.GetByIdForProposalResponseForUpdateAsync(orderId, ct)
            ?? throw new KeyNotFoundException("Encomenda não encontrada.");

        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("Encomenda não pertence ao cliente.");

        var proposal = order.ChangeProposals.FirstOrDefault(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Proposta não encontrada.");

        if (proposal.Status != OrderChangeProposalStatus.Pending)
            throw new InvalidOperationException("Esta proposta já foi respondida.");

        if (accepted)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async inner =>
            {
            proposal.Status = OrderChangeProposalStatus.AcceptedByCustomer;
            ApplyProposedChanges(order, proposal);
            await OrderPricing.ReapplyOrClearAsync(_unitOfWork, order, proposal.RevisedTotal.HasValue, ct);
            if (proposal.RevisedTotal.HasValue)
                order.EstimatedTotal = proposal.RevisedTotal.Value;

            var from = order.Status;
            OrderStateMachine.EnsureCanTransition(from, OrderStatus.Confirmed);
            order.Status = OrderStatus.Confirmed;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.Add(new OrderStatusHistory
            {
                TenantId = order.TenantId,
                OrderId = order.Id,
                FromStatus = from,
                ToStatus = OrderStatus.Confirmed,
                ChangedByUserId = customerId,
                Notes = notes ?? "Cliente aceitou a proposta de alteração."
            });

            foreach (var other in order.ChangeProposals.Where(p => p.Id != proposal.Id && p.Status == OrderChangeProposalStatus.Pending))
                other.Status = OrderChangeProposalStatus.WithdrawnByFlorist;

            if (_stock is null)
                await _unitOfWork.SaveChangesAsync(ct);
            else
                await _stock.WithFlowerLocksAsync(order, async inner =>
                {
                    await _stock.ApplyConfirmAsync(order, inner);
                    await _unitOfWork.SaveChangesAsync(inner);
                }, ct);
            if (_stock is not null)
                await _stock.FlushNoticesAsync(ct);
            await _audit.LogAsync("OrderChangeAccepted", customerId, order.TenantId, nameof(OrderChangeProposal), proposal.Id, notes, inner);
            }, ct);
            await NotifyOrderConfirmedAsync(orderId, ct);
        }
        else
        {
            proposal.Status = OrderChangeProposalStatus.RejectedByCustomer;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.Add(new OrderStatusHistory
            {
                TenantId = order.TenantId,
                OrderId = order.Id,
                FromStatus = order.Status,
                ToStatus = order.Status,
                ChangedByUserId = customerId,
                Notes = notes ?? "Cliente recusou a proposta de alteração."
            });
            await _unitOfWork.SaveChangesAsync(ct);
            await _audit.LogAsync("OrderChangeRejected", customerId, order.TenantId, nameof(OrderChangeProposal), proposal.Id, notes, ct);
        }

        return Map(await LoadOrderAsync(orderId, ct));
    }

    private static void ApplyProposedChanges(Order order, OrderChangeProposal proposal)
    {
        if (proposal.RevisedTotal.HasValue)
            order.EstimatedTotal = proposal.RevisedTotal.Value;

        if (string.IsNullOrWhiteSpace(proposal.ProposedItemChangesJson))
            return;

        if (!OrderItemChangesJsonValidator.TryParse(proposal.ProposedItemChangesJson, out var items))
            throw new InvalidOperationException("JSON de itens propostos inválido.");

        var previousRates = order.Items
            .Where(i => i.FlowerId.HasValue)
            .GroupBy(i => i.FlowerId!.Value)
            .ToDictionary(g => g.Key, g => g.First().VatRate);
        order.Items.Clear();
        foreach (var item in items)
        {
            order.Items.Add(new OrderItem
            {
                TenantId = order.TenantId,
                FlowerId = item.FlowerId,
                Name = item.Name.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                VatRate = item.FlowerId is Guid flowerId && previousRates.TryGetValue(flowerId, out var rate) ? rate : 23m
            });
        }

        order.EstimatedTotal = order.Items.Sum(i => i.UnitPrice * i.Quantity) + order.DeliveryFee;
    }

    private async Task<decimal> DefaultVatRateAsync(CancellationToken ct)
    {
        var rate = await _unitOfWork.InvoicingSettings.Query().Select(s => (decimal?)s.DefaultVatRate).FirstOrDefaultAsync(ct);
        return rate is decimal value && VatRates.IsAllowed(value) ? value : VatRates.StandardMainland;
    }

    private async Task<Dictionary<Guid, decimal?>> FlowerVatRatesAsync(IEnumerable<Guid?> flowerIds, CancellationToken ct)
    {
        var ids = flowerIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
            return [];
        return await _unitOfWork.Flowers.Query()
            .Where(f => ids.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.VatRate, ct);
    }

    private static decimal ResolveVat(Guid? flowerId, IReadOnlyDictionary<Guid, decimal?> rates, decimal fallback) =>
        flowerId is Guid id && rates.TryGetValue(id, out var rate) && rate is decimal value && VatRates.IsAllowed(value)
            ? value
            : fallback;

    private async Task ValidateOrderReferencesAsync(Guid tenantId, CreateOrderRequest request, CancellationToken ct)
    {
        if (request.InspiredByPortfolioArrangementId is Guid portfolioId)
        {
            if (!await _unitOfWork.Portfolio.ExistsAsync(portfolioId, ct))
                throw new InvalidOperationException("Arranjo de inspiração não encontrado.");
        }

        var flowerIds = request.Items.Where(i => i.FlowerId.HasValue).Select(i => i.FlowerId!.Value).Distinct().ToList();
        if (flowerIds.Count > 0)
        {
            var known = await _unitOfWork.Flowers.FilterExistingIdsAsync(flowerIds, ct);
            if (known.Count != flowerIds.Count)
                throw new InvalidOperationException("Uma ou mais flores do catálogo não existem.");
        }
    }

    private void EnsureCanAccess(Order order, Guid userId, string role)
    {
        if (role is FloristHub.DAL.Constants.Roles.Florist)
        {
            var tenantId = _tenant.TenantId
                ?? throw new UnauthorizedAccessException("Sem acesso a esta encomenda.");
            if (order.TenantId != tenantId)
                throw new UnauthorizedAccessException("Sem acesso a esta encomenda.");
            return;
        }

        if (order.CustomerId != userId)
            throw new UnauthorizedAccessException("Sem acesso a esta encomenda.");
    }

    private async Task<Order> LoadOrderAsync(Guid id, CancellationToken ct) =>
        await _unitOfWork.Orders.GetByIdWithDetailsAsync(id, ct)
        ?? throw new KeyNotFoundException("Encomenda não encontrada.");

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");

    private async Task<Tenant> LoadTenantAsync(Guid tenantId, CancellationToken ct) =>
        await _unitOfWork.Tenants.GetByIdReadOnlyAsync(tenantId, ct)
        ?? throw new InvalidOperationException("Loja não encontrada.");

    private async Task NotifyOrderConfirmedAsync(Guid orderId, CancellationToken ct)
    {
        var order = await LoadOrderAsync(orderId, ct);
        var tenant = await LoadTenantAsync(order.TenantId, ct);
        _emails.OrderConfirmed(order, tenant);
        _push.OrderStatusChanged(order, tenant, OrderStatus.Confirmed);
    }

    private async Task NotifyOrderRejectedAsync(Guid orderId, string reason, CancellationToken ct)
    {
        var order = await LoadOrderAsync(orderId, ct);
        var tenant = await LoadTenantAsync(order.TenantId, ct);
        _emails.OrderRejected(order, tenant, reason);
    }

    private async Task NotifyStatusTransitionAsync(Guid orderId, OrderStatus to, string? notes, bool publishPush, CancellationToken ct)
    {
        var wantsPush = publishPush && to is OrderStatus.Confirmed or OrderStatus.InPreparation or OrderStatus.Ready or OrderStatus.Delivered;
        var wantsEmail = to is OrderStatus.InPreparation or OrderStatus.Ready or OrderStatus.Delivered or OrderStatus.Cancelled;
        if (!wantsPush && !wantsEmail)
            return;

        var order = await LoadOrderAsync(orderId, ct);
        var tenant = await LoadTenantAsync(order.TenantId, ct);
        if (wantsPush)
            _push.OrderStatusChanged(order, tenant, to);
        if (!wantsEmail)
            return;

        switch (to)
        {
            case OrderStatus.InPreparation:
                _emails.OrderInPreparation(order, tenant);
                break;
            case OrderStatus.Ready:
                _emails.OrderReady(order, tenant);
                break;
            case OrderStatus.Delivered:
                _emails.OrderDelivered(order, tenant);
                break;
            case OrderStatus.Cancelled:
                _emails.OrderCancelled(order, tenant, notes);
                break;
        }
    }

    private static OrderDto Map(Order o) =>
        new(
            o.Id,
            o.Status,
            o.BaseType,
            o.DeliveryType,
            o.DeliveryAddress,
            o.RequestedDeliveryAt,
            o.CustomerNotes,
            o.InspiredByPortfolioArrangementId,
            o.EstimatedTotal,
            o.DeliveryFee,
            o.DeliveryZoneId,
            o.DeliveryZone?.Name,
            o.PaymentCaptured,
            o.Arrangement3DPreviewToken,
            o.Customer?.FullName,
            o.Customer?.Email,
            o.CreatedAt,
            o.Items.Select(i => new OrderItemDto(i.Id, i.FlowerId, i.Name, i.Quantity, i.UnitPrice, i.VatRate)).ToList(),
            (o.StatusHistory ?? []).OrderBy(h => h.CreatedAt).Select(h => new OrderStatusHistoryDto(h.Id, h.FromStatus, h.ToStatus, h.CreatedAt, h.Notes)).ToList(),
            (o.ChangeProposals ?? []).OrderByDescending(c => c.CreatedAt).Select(c => new OrderChangeProposalDto(c.Id, c.Status, c.Message, c.ProposedItemChangesJson, c.RevisedTotal, c.CreatedAt)).ToList(),
            o.ArrangementShape,
            o.ArrangementVariationSeed,
            o.ArrangementPreviewUrl,
            MapPayment(o),
            o.CustomerTaxId,
            o.DiscountAmount,
            o.CouponCode,
            o.Review is null
                ? null
                : new OrderReviewSummaryDto(o.Review.Id, o.Review.Stars, o.Review.Comment, o.Review.FloristReply, o.Review.IsHidden),
            o.TrackingAccessToken);

    private static OrderPaymentDto? MapPayment(Order o)
    {
        var payment = o.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        return payment is null
            ? null
            : new OrderPaymentDto(payment.Id, payment.Status, payment.Amount, payment.RefundedAmount, payment.Currency);
    }
}
