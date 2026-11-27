using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Florist;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services;

public interface IFloristDashboardService
{
    Task<FloristDashboardDto> GetAsync(CancellationToken ct = default);

    Task<FloristDashboardMetricsDto> GetMetricsAsync(int days, CancellationToken ct = default);
}

public class FloristDashboardService : IFloristDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;

    public FloristDashboardService(IUnitOfWork unitOfWork, ITenantContext tenant)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
    }

    public async Task<FloristDashboardDto> GetAsync(CancellationToken ct = default)
    {
        RequireTenant();
        var utcNow = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(utcNow.Date, TimeSpan.Zero);
        var tomorrowStart = todayStart.AddDays(1);
        var dayAfterTomorrow = tomorrowStart.AddDays(1);
        var monthStart = new DateTimeOffset(utcNow.Year, utcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var orders = _unitOfWork.Orders.Query();

        var byStatus = await orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var statusMap = byStatus.ToDictionary(
            x => ((int)x.Status).ToString(),
            x => x.Count,
            StringComparer.Ordinal);

        var activePrepStatuses = new[]
        {
            OrderStatus.Confirmed,
            OrderStatus.InPreparation,
            OrderStatus.Ready
        };

        var ordersToday = await orders.CountAsync(o =>
            o.RequestedDeliveryAt >= todayStart && o.RequestedDeliveryAt < tomorrowStart &&
            activePrepStatuses.Contains(o.Status), ct);

        var ordersTomorrow = await orders.CountAsync(o =>
            o.RequestedDeliveryAt >= tomorrowStart && o.RequestedDeliveryAt < dayAfterTomorrow &&
            activePrepStatuses.Contains(o.Status), ct);

        var monthlyRevenue = await orders
            .Where(o => o.CreatedAt >= monthStart && o.PaymentCaptured)
            .SumAsync(o => o.EstimatedTotal, ct);

        var ordersWithItems = await orders.Include(o => o.Items).AsNoTracking().ToListAsync(ct);
        var topItems = ordersWithItems
            .SelectMany(o => o.Items)
            .GroupBy(i => i.Name)
            .Select(g => new TopCatalogItemDto(
                g.Key,
                g.Count(),
                g.Sum(x => x.UnitPrice * x.Quantity)))
            .OrderByDescending(x => x.OrderLineCount)
            .Take(5)
            .ToList();

        return new FloristDashboardDto(statusMap, ordersToday, ordersTomorrow, monthlyRevenue, topItems);
    }

    public async Task<FloristDashboardMetricsDto> GetMetricsAsync(int days, CancellationToken ct = default)
    {
        RequireTenant();
        days = days switch
        {
            7 or 30 or 90 => days,
            _ => 30,
        };

        var utcNow = DateTimeOffset.UtcNow;
        var today = utcNow.Date;
        var periodStart = today.AddDays(-(days - 1));
        var periodStartOffset = new DateTimeOffset(periodStart, TimeSpan.Zero);

        var orders = _unitOfWork.Orders.Query();

        var pendingOrders = await orders.CountAsync(o => o.Status == OrderStatus.PendingConfirmation, ct);

        var inPeriod = orders.Where(o => o.CreatedAt >= periodStartOffset);

        var orderGroups = await inPeriod
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var revenueGroups = await inPeriod
            .Where(o => o.PaymentCaptured)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.EstimatedTotal) })
            .ToListAsync(ct);

        var ordersByDay = BuildDailyCounts(periodStart, days, orderGroups.ToDictionary(x => x.Date, x => x.Count));
        var revenueByDay = BuildDailyRevenue(periodStart, days, revenueGroups.ToDictionary(x => x.Date, x => x.Revenue));

        var periodRevenue = await inPeriod
            .Where(o => o.PaymentCaptured)
            .SumAsync(o => o.EstimatedTotal, ct);

        var ordersWithItems = await inPeriod.Include(o => o.Items).AsNoTracking().ToListAsync(ct);
        var topFlowers = ordersWithItems
            .SelectMany(o => o.Items)
            .Where(i => i.FlowerId.HasValue)
            .GroupBy(i => i.Name)
            .Select(g => new TopCatalogItemDto(
                g.Key,
                g.Count(),
                g.Sum(x => x.UnitPrice * x.Quantity)))
            .OrderByDescending(x => x.OrderLineCount)
            .Take(5)
            .ToList();

        return new FloristDashboardMetricsDto(
            days,
            pendingOrders,
            periodRevenue,
            ordersByDay,
            revenueByDay,
            topFlowers);
    }

    public static IReadOnlyList<DailyCountDto> BuildDailyCounts(
        DateTime periodStart,
        int days,
        IReadOnlyDictionary<DateTime, int> counts)
    {
        var list = new List<DailyCountDto>(days);
        for (var i = 0; i < days; i++)
        {
            var day = periodStart.AddDays(i);
            counts.TryGetValue(day, out var count);
            list.Add(new DailyCountDto(day.ToString("yyyy-MM-dd"), count));
        }

        return list;
    }

    public static IReadOnlyList<DailyRevenueDto> BuildDailyRevenue(
        DateTime periodStart,
        int days,
        IReadOnlyDictionary<DateTime, decimal> revenue)
    {
        var list = new List<DailyRevenueDto>(days);
        for (var i = 0; i < days; i++)
        {
            var day = periodStart.AddDays(i);
            revenue.TryGetValue(day, out var amount);
            list.Add(new DailyRevenueDto(day.ToString("yyyy-MM-dd"), amount));
        }

        return list;
    }

    private void RequireTenant()
    {
        if (!_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Tenant não resolvido.");
    }
}
