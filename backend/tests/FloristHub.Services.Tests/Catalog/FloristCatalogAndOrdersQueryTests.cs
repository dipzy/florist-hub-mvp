using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.Repositories;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Tests.Catalog;

public class FloristCatalogAndOrdersQueryTests
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Manage_catalog_returns_rows_and_image_for_the_tenant()
    {
        var (db, flowers) = await SeedAsync();
        db.Flowers.Add(new Flower
        {
            TenantId = TenantA,
            Name = "Rosa",
            UnitPrice = 2.5m,
            ImageUrl = "https://cdn.example/rosa.jpg",
            IsCatalogActive = true,
            StockQuantity = 8,
        });
        db.Flowers.Add(new Flower
        {
            TenantId = Guid.NewGuid(),
            Name = "Outra loja",
            UnitPrice = 1m,
            IsCatalogActive = true,
        });
        await db.SaveChangesAsync();

        var page = await flowers.ListManageAsync(null, null, 1, 10);

        Assert.Equal(1, page.Total);
        var item = Assert.Single(page.Items);
        Assert.Equal("Rosa", item.Name);
        Assert.Equal("https://cdn.example/rosa.jpg", item.ImageUrl);
    }

    [Fact]
    public async Task Florist_order_queries_return_pending_and_confirmed()
    {
        var (db, _) = await SeedAsync();
        db.Orders.Add(new Order
        {
            TenantId = TenantA,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.PendingConfirmation,
            DeliveryType = DeliveryType.StorePickup,
            EstimatedTotal = 15m,
            CreatedAt = DateTimeOffset.Parse("2026-11-11T10:00:00Z"),
        });
        db.Orders.Add(new Order
        {
            TenantId = TenantA,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Confirmed,
            DeliveryType = DeliveryType.HomeDelivery,
            EstimatedTotal = 28m,
            CreatedAt = DateTimeOffset.Parse("2026-11-12T10:00:00Z"),
        });
        await db.SaveChangesAsync();

        var stored = await db.Orders.AsNoTracking().OrderBy(o => o.CreatedAt).ToListAsync();
        var repo = new OrderRepository(db);
        var counted = await repo.QueryWithDetails().CountAsync();
        var paged = await repo.SearchForFloristPagedAsync(null, null, null, null, null, 1, 10);

        Assert.Equal(2, stored.Count);
        Assert.Contains(stored, o => o.Status == OrderStatus.PendingConfirmation);
        Assert.Contains(stored, o => o.Status == OrderStatus.Confirmed);
        Assert.Equal(2, counted);
        Assert.Equal(2, paged.Total);
    }

    private static async Task<(ApplicationDbContext Db, FlowerService Flowers)> SeedAsync()
    {
        var tenant = new TenantContext();
        tenant.SetTenant(TenantA, "sao-flores");
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        await db.Database.EnsureCreatedAsync();
        return (db, new FlowerService(new UnitOfWork(db), tenant, new NullAudit()));
    }

    private sealed class NullAudit : IAuditLogger
    {
        public Task LogAsync(
            string action,
            Guid? userId,
            Guid? tenantId,
            string? entityType = null,
            Guid? entityId = null,
            string? details = null,
            CancellationToken ct = default) => Task.CompletedTask;
    }
}
