using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Tests;

public class FloristDashboardServiceTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void BuildDailyCounts_fills_missing_days_with_zero()
    {
        var start = new DateTime(2026, 1, 1);
        var counts = new Dictionary<DateTime, int> { [start.AddDays(1)] = 3 };

        var series = FloristDashboardService.BuildDailyCounts(start, 3, counts);

        Assert.Equal(3, series.Count);
        Assert.Equal(0, series[0].Count);
        Assert.Equal(3, series[1].Count);
        Assert.Equal(0, series[2].Count);
    }

    [Fact]
    public async Task GetMetricsAsync_isolates_tenants()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(TenantA);
        await using var db = CreateDb(tenantContext);

        var flowerA = Guid.NewGuid();
        var flowerB = Guid.NewGuid();
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();

        db.Tenants.AddRange(
            new Tenant { Id = TenantA, Name = "A", Slug = "tenant-a", IsActive = true },
            new Tenant { Id = TenantB, Name = "B", Slug = "tenant-b", IsActive = true });

        db.Flowers.AddRange(
            new Flower { Id = flowerA, TenantId = TenantA, Name = "Rosa", UnitPrice = 2m, Unit = "ramo", IsCatalogActive = true },
            new Flower { Id = flowerB, TenantId = TenantB, Name = "Rosa B", UnitPrice = 2m, Unit = "ramo", IsCatalogActive = true });

        var orderA = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            CustomerId = customerA,
            Status = OrderStatus.PendingConfirmation,
            BaseType = ArrangementBaseType.Sponge,
            DeliveryType = DeliveryType.StorePickup,
            EstimatedTotal = 20m,
            PaymentCaptured = true,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
        };
        orderA.Items.Add(new OrderItem
        {
            TenantId = TenantA,
            FlowerId = flowerA,
            Name = "Rosa",
            Quantity = 5,
            UnitPrice = 2m,
        });

        var orderB = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = TenantB,
            CustomerId = customerB,
            Status = OrderStatus.Confirmed,
            BaseType = ArrangementBaseType.Sponge,
            DeliveryType = DeliveryType.StorePickup,
            EstimatedTotal = 99m,
            PaymentCaptured = true,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        orderB.Items.Add(new OrderItem
        {
            TenantId = TenantB,
            FlowerId = flowerB,
            Name = "Rosa B",
            Quantity = 1,
            UnitPrice = 99m,
        });

        db.Orders.AddRange(orderA, orderB);
        await db.SaveChangesAsync();

        var service = new FloristDashboardService(new UnitOfWork(db), tenantContext);
        var metrics = await service.GetMetricsAsync(7);

        Assert.Equal(1, metrics.PendingOrders);
        Assert.Equal(20m, metrics.PeriodRevenue);
        Assert.Single(metrics.TopFlowers);
        Assert.Equal("Rosa", metrics.TopFlowers[0].Name);
        Assert.Equal(1, metrics.OrdersByDay.Sum(d => d.Count));
    }

    private static ApplicationDbContext CreateDb(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, tenantContext);
    }
}
