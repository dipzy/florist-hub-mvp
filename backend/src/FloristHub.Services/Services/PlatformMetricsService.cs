using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.Persistence;
using FloristHub.Services.DTOs.Florist;
using FloristHub.Services.DTOs.Platform;
using FloristHub.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services;

public interface IPlatformMetricsService
{
    Task<PlatformMetricsDto> GetAsync(int days, CancellationToken ct = default);
}

/// <summary>Métricas globais — ignora filtros de tenant de forma explícita (apenas SuperAdmin na API).</summary>
public sealed class PlatformMetricsService : IPlatformMetricsService
{
    private readonly ApplicationDbContext _db;

    public PlatformMetricsService(ApplicationDbContext db) => _db = db;

    public async Task<PlatformMetricsDto> GetAsync(int days, CancellationToken ct = default)
    {
        days = days switch
        {
            7 or 30 or 90 => days,
            _ => 30
        };

        var utcNow = DateTimeOffset.UtcNow;
        var today = utcNow.Date;
        var periodStart = today.AddDays(-(days - 1));
        var periodStartOffset = new DateTimeOffset(periodStart, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(utcNow.Year, utcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var tenants = _db.Tenants.IgnoreQueryFilters().AsNoTracking();
        var orders = _db.Orders.IgnoreQueryFilters().AsNoTracking();
        var payments = _db.Payments.IgnoreQueryFilters().AsNoTracking();

        var activeStores = await tenants.CountAsync(t => t.IsActive, ct);
        var stripeConnected = await tenants.CountAsync(
            t => t.StripeAccountId != null && t.StripeChargesEnabled, ct);
        var newStoresThisMonth = await tenants.CountAsync(t => t.CreatedAt >= monthStart, ct);

        var inPeriodOrders = orders.Where(o => o.CreatedAt >= periodStartOffset);
        var ordersInPeriod = await inPeriodOrders.CountAsync(ct);

        var capturedInPeriod = inPeriodOrders.Where(o => o.PaymentCaptured);
        var gmv = await capturedInPeriod.SumAsync(o => o.EstimatedTotal, ct);

        var succeededPayments = payments.Where(p =>
            p.Status == PaymentStatus.Succeeded && p.CreatedAt >= periodStartOffset);
        var commissionCents = await succeededPayments.SumAsync(p => p.ApplicationFeeAmountCents, ct);
        var commission = CommissionCalculator.FromCents(commissionCents);

        var orderGroups = await inPeriodOrders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var revenueGroups = await capturedInPeriod
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.EstimatedTotal) })
            .ToListAsync(ct);

        var ordersByDay = FloristDashboardService.BuildDailyCounts(
            periodStart,
            days,
            orderGroups.ToDictionary(x => x.Date, x => x.Count))
            .Select(d => new PlatformDailyCountDto(d.Date, d.Count))
            .ToList();

        var gmvByDay = FloristDashboardService.BuildDailyRevenue(
            periodStart,
            days,
            revenueGroups.ToDictionary(x => x.Date, x => x.Revenue))
            .Select(d => new PlatformDailyAmountDto(d.Date, d.Revenue))
            .ToList();

        var topAgg = await capturedInPeriod
            .GroupBy(o => o.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                OrderCount = g.Count(),
                Gmv = g.Sum(o => o.EstimatedTotal),
            })
            .OrderByDescending(x => x.Gmv)
            .Take(10)
            .ToListAsync(ct);

        var topTenantIds = topAgg.Select(x => x.TenantId).ToList();
        var tenantMap = topTenantIds.Count == 0
            ? new Dictionary<Guid, Tenant>()
            : await tenants.Where(t => topTenantIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);

        var topStores = topAgg
            .Select(row =>
            {
                tenantMap.TryGetValue(row.TenantId, out var tenant);
                return new PlatformTopStoreDto(
                    row.TenantId,
                    tenant?.Name ?? "—",
                    tenant?.Slug ?? "—",
                    row.OrderCount,
                    row.Gmv);
            })
            .ToList();

        var twelveMonthsAgo = monthStart.AddMonths(-11);
        var newStoreGroups = await tenants
            .Where(t => t.CreatedAt >= twelveMonthsAgo)
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var newStoresByMonth = new List<PlatformDailyCountDto>(12);
        for (var i = 0; i < 12; i++)
        {
            var month = twelveMonthsAgo.AddMonths(i);
            var count = newStoreGroups
                .Where(x => x.Year == month.Year && x.Month == month.Month)
                .Select(x => x.Count)
                .FirstOrDefault();
            newStoresByMonth.Add(new PlatformDailyCountDto($"{month:yyyy-MM}", count));
        }

        return new PlatformMetricsDto(
            days,
            activeStores,
            ordersInPeriod,
            gmv,
            commission,
            stripeConnected,
            newStoresThisMonth,
            ordersByDay,
            gmvByDay,
            topStores,
            newStoresByMonth);
    }
}
