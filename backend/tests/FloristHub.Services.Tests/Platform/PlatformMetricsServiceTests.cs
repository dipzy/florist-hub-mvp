using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.Services;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Tests.Platform;

public class PlatformMetricsServiceTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task GetAsync_aggregates_across_tenants_with_ignore_query_filters()
    {
        await using var db = CreateDb();
        db.Tenants.AddRange(
            new Tenant { Id = TenantA, Name = "Loja A", Slug = "a", IsActive = true, StripeAccountId = "acct_a", StripeChargesEnabled = true },
            new Tenant { Id = TenantB, Name = "Loja B", Slug = "b", IsActive = true });

        db.Orders.AddRange(
            new Order
            {
                TenantId = TenantA,
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.Delivered,
                BaseType = ArrangementBaseType.Sponge,
                DeliveryType = DeliveryType.StorePickup,
                EstimatedTotal = 50m,
                DeliveryFee = 0m,
                PaymentCaptured = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new Order
            {
                TenantId = TenantB,
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.Delivered,
                BaseType = ArrangementBaseType.Sponge,
                DeliveryType = DeliveryType.StorePickup,
                EstimatedTotal = 80m,
                DeliveryFee = 5m,
                PaymentCaptured = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            });

        await db.SaveChangesAsync();

        var sut = new PlatformMetricsService(db);
        var metrics = await sut.GetAsync(30);

        Assert.Equal(2, metrics.ActiveStores);
        Assert.Equal(2, metrics.OrdersInPeriod);
        Assert.Equal(130m, metrics.GmvInPeriod);
        Assert.Equal(1, metrics.StoresWithStripeConnected);
    }

    [Fact]
    public async Task Tenant_scoped_context_does_not_see_other_tenant_orders_in_repository()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(TenantA);
        await using var db = CreateDb(tenantContext);

        db.Tenants.AddRange(
            new Tenant { Id = TenantA, Name = "Loja A", Slug = "a", IsActive = true },
            new Tenant { Id = TenantB, Name = "Loja B", Slug = "b", IsActive = true });

        db.Orders.AddRange(
            new Order
            {
                TenantId = TenantA,
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.PendingConfirmation,
                BaseType = ArrangementBaseType.Sponge,
                DeliveryType = DeliveryType.StorePickup,
                EstimatedTotal = 10m,
                PaymentCaptured = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Order
            {
                TenantId = TenantB,
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.PendingConfirmation,
                BaseType = ArrangementBaseType.Sponge,
                DeliveryType = DeliveryType.StorePickup,
                EstimatedTotal = 999m,
                PaymentCaptured = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        await db.SaveChangesAsync();

        var count = await db.Orders.CountAsync();
        Assert.Equal(1, count);
    }

    private static ApplicationDbContext CreateDb(ITenantContext? tenantContext = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, tenantContext ?? new TenantContext());
    }
}
