using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Orders;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Orders;

public interface IOrderTrackingService
{
    Task<PublicOrderTrackingDto?> GetByTokenAsync(string token, CancellationToken ct = default);
}

public sealed class OrderTrackingService : IOrderTrackingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;

    public OrderTrackingService(IUnitOfWork unitOfWork, ITenantContext tenant)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
    }

    public async Task<PublicOrderTrackingDto?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        if (!OrderTrackingToken.LooksValid(token))
            return null;

        var tenantId = _tenant.TenantId;
        if (tenantId is null)
            return null;

        var normalized = token.Trim();
        var order = await _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.TrackingAccessToken == normalized && o.TenantId == tenantId, ct);
        if (order is null)
            return null;

        var window = await ResolveWindowAsync(order, ct);
        return Map(order, window);
    }

    private async Task<DeliveryCapacityWindow?> ResolveWindowAsync(Order order, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(order.CapacityWindowKey))
            return null;
        var windows = await _unitOfWork.CapacityWindows.Query()
            .AsNoTracking()
            .Where(w => w.TenantId == order.TenantId)
            .ToListAsync(ct);
        return windows.FirstOrDefault(w => w.Key == order.CapacityWindowKey);
    }

    private static PublicOrderTrackingDto Map(Order order, DeliveryCapacityWindow? window) =>
        new(
            order.Id,
            order.Status,
            order.DeliveryType,
            order.RequestedDeliveryAt,
            order.CapacityServiceDate,
            window?.Label,
            window?.StartTime.ToString("HH:mm"),
            window?.EndTime.ToString("HH:mm"),
            order.StatusHistory
                .OrderBy(h => h.CreatedAt)
                .Select(h => new PublicOrderStatusStepDto(h.ToStatus, h.CreatedAt))
                .ToList());
}
