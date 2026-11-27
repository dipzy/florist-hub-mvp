using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Notifications;

public interface IFloristPanelNotificationService
{
    Task RecordNewOrderAsync(Guid tenantId, Guid orderId, CancellationToken ct = default);
    Task RecordPaymentReceivedAsync(Guid tenantId, Guid orderId, CancellationToken ct = default);
    Task<FloristPanelNotificationSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FloristPanelNotificationDto>> ListUnreadAsync(int limit, CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
}

public sealed class FloristPanelNotificationService : IFloristPanelNotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;

    public FloristPanelNotificationService(IUnitOfWork unitOfWork, ITenantContext tenant)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
    }

    public Task RecordNewOrderAsync(Guid tenantId, Guid orderId, CancellationToken ct = default) =>
        RecordAsync(tenantId, orderId, FloristPanelNotificationKind.NewOrder, ct);

    public Task RecordPaymentReceivedAsync(Guid tenantId, Guid orderId, CancellationToken ct = default) =>
        RecordAsync(tenantId, orderId, FloristPanelNotificationKind.PaymentReceived, ct);

    public async Task<FloristPanelNotificationSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        RequireTenant();
        var unread = await _unitOfWork.FloristPanelNotifications.Query()
            .CountAsync(n => n.ReadAt == null, ct);
        return new FloristPanelNotificationSummaryDto(unread);
    }

    public async Task<IReadOnlyList<FloristPanelNotificationDto>> ListUnreadAsync(int limit, CancellationToken ct = default)
    {
        RequireTenant();
        var take = Math.Clamp(limit, 1, 50);
        var rows = await _unitOfWork.FloristPanelNotifications.Query()
            .AsNoTracking()
            .Include(n => n.Order)
            .Where(n => n.ReadAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        RequireTenant();
        var pending = await _unitOfWork.FloristPanelNotifications.Query()
            .Where(n => n.ReadAt == null)
            .ToListAsync(ct);
        if (pending.Count == 0)
            return;
        var now = DateTimeOffset.UtcNow;
        foreach (var row in pending)
        {
            row.ReadAt = now;
            row.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task RecordAsync(Guid tenantId, Guid orderId, FloristPanelNotificationKind kind, CancellationToken ct)
    {
        var exists = await _unitOfWork.FloristPanelNotifications.Query()
            .IgnoreQueryFilters()
            .AnyAsync(n => n.TenantId == tenantId && n.OrderId == orderId && n.Kind == kind && n.ReadAt == null, ct);
        if (exists)
            return;

        await _unitOfWork.FloristPanelNotifications.AddAsync(new FloristPanelNotification
        {
            TenantId = tenantId,
            OrderId = orderId,
            Kind = kind,
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Loja não resolvida.");

    private static FloristPanelNotificationDto Map(FloristPanelNotification row) =>
        new(
            row.Id,
            row.OrderId,
            row.Kind,
            row.CreatedAt,
            row.Order?.Status ?? OrderStatus.PendingConfirmation,
            row.Order?.EstimatedTotal ?? 0,
            row.Order is null ? null : $"#{row.Order.Id.ToString()[..8]}");
}

public record FloristPanelNotificationSummaryDto(int UnreadCount);

public record FloristPanelNotificationDto(
    Guid Id,
    Guid OrderId,
    FloristPanelNotificationKind Kind,
    DateTimeOffset CreatedAt,
    OrderStatus OrderStatus,
    decimal EstimatedTotal,
    string? ShortRef);
