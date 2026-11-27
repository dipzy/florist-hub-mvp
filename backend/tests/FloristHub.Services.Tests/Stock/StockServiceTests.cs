using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Capacity;
using FloristHub.Services.Email;
using FloristHub.Services.Push;
using FloristHub.Services.Stock;
using FloristHub.Services.Tests.Email;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Tests.Stock;

public class StockServiceTests
{
    [Fact]
    public async Task Confirm_decrements_stock_and_cancel_or_refund_restores_it_once()
    {
        var fx = await SeedAsync(8, 3);
        var order = await fx.OrderAsync(5);

        await fx.ConfirmAsync(order.Id);
        Assert.Equal(3, await fx.QuantityAsync());
        Assert.Single(await fx.Stock.ListMovementsAsync(fx.FlowerId));

        var cancelled = await fx.ReloadAsync(order.Id);
        await fx.Stock.WithFlowerLocksAsync(cancelled, async ct =>
        {
            await fx.Stock.ApplyRestoreAsync(cancelled, StockMovementReason.OrderCancelled, ct);
            await fx.Db.SaveChangesAsync(ct);
        });
        Assert.Equal(8, await fx.QuantityAsync());
        Assert.False((await fx.ReloadAsync(order.Id)).StockCommitted);

        await fx.Stock.ApplyRestoreAsync(await fx.ReloadAsync(order.Id), StockMovementReason.OrderRefunded);
        await fx.Db.SaveChangesAsync();
        Assert.Equal(8, await fx.QuantityAsync());
    }

    [Fact]
    public async Task Refund_after_confirm_restores_stock_and_a_second_refund_does_not()
    {
        var fx = await SeedAsync(4, 1);
        var order = await fx.OrderAsync(2);
        await fx.ConfirmAsync(order.Id);
        await fx.Stock.ApplyRestoreForOrderAsync(order.Id, StockMovementReason.OrderRefunded);
        await fx.Db.SaveChangesAsync();
        await fx.Stock.ApplyRestoreForOrderAsync(order.Id, StockMovementReason.OrderRefunded);
        await fx.Db.SaveChangesAsync();

        Assert.Equal(4, await fx.QuantityAsync());
        var reasons = await fx.Db.StockMovements.Select(m => m.Reason).ToListAsync();
        Assert.Contains(StockMovementReason.OrderConfirmed, reasons);
        Assert.Equal(1, reasons.Count(r => r == StockMovementReason.OrderRefunded));
    }

    [Fact]
    public async Task Insufficient_stock_does_not_change_the_balance()
    {
        var fx = await SeedAsync(1, 0);
        var order = await fx.OrderAsync(2);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fx.ConfirmAsync(order.Id));
        Assert.Equal(1, await fx.QuantityAsync());
        Assert.False((await fx.ReloadAsync(order.Id)).StockCommitted);
    }

    [Fact]
    public async Task Crossing_the_minimum_sends_email_and_push_once()
    {
        var fx = await SeedAsync(4, 3);
        var order = await fx.OrderAsync(2);
        await fx.ConfirmAsync(order.Id);
        await fx.Stock.FlushNoticesAsync();

        Assert.Equal(2, await fx.QuantityAsync());
        Assert.Contains(fx.Emails.Messages, m => m.To == "florista@loja.pt" && m.Subject.Contains("Stock baixo"));
        Assert.Contains(fx.Push.Items, p => p.Kind == NotificationPreferenceKind.LowStock && p.OrderId == Guid.Empty);

        var second = await fx.OrderAsync(1);
        await fx.ConfirmAsync(second.Id);
        await fx.Stock.FlushNoticesAsync();
        Assert.Single(fx.Emails.Messages);
        Assert.Single(fx.Push.Items);
    }

    [Fact]
    public async Task Parallel_confirms_cannot_oversell_the_last_stem()
    {
        var dbName = Guid.NewGuid().ToString();
        var gate = new StockGate();
        Guid flowerId;
        var orderIds = new List<Guid>();
        var seed = NewDb(dbName);
        await using (seed.Db)
        {
            var flower = new Flower { TenantId = seed.Tenant.TenantId!.Value, Name = "Rosa", UnitPrice = 1, StockQuantity = 1, MinimumStockQuantity = 0 };
            seed.Db.Flowers.Add(flower);
            await seed.Db.SaveChangesAsync();
            flowerId = flower.Id;
            for (var i = 0; i < 6; i++)
            {
                var order = Order(seed.Tenant.TenantId!.Value, flowerId, 1);
                seed.Db.Orders.Add(order);
                orderIds.Add(order.Id);
            }

            await seed.Db.SaveChangesAsync();
        }

        var results = await Task.WhenAll(orderIds.Select(id => Task.Run(async () =>
        {
            var opened = NewDb(dbName);
            await using var db = opened.Db;
            var stock = StockFor(opened, gate, new FakeEmailQueue(), new ListPushQueue());
            try
            {
                var order = await db.Orders.Include(o => o.Items).SingleAsync(o => o.Id == id);
                await stock.WithFlowerLocksAsync(order, async ct =>
                {
                    await stock.ApplyConfirmAsync(order, ct);
                    await db.SaveChangesAsync(ct);
                });
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        })));

        Assert.Equal(1, results.Count(ok => ok));
        var check = NewDb(dbName);
        await using var checkDb = check.Db;
        var left = await checkDb.Flowers.IgnoreQueryFilters().SingleAsync(f => f.Id == flowerId);
        Assert.Equal(0, left.StockQuantity);
    }

    [Fact]
    public async Task Movements_of_another_tenant_are_hidden()
    {
        var opened = NewDb(Guid.NewGuid().ToString());
        await using var db = opened.Db;
        var other = Guid.NewGuid();
        var flower = new Flower { TenantId = other, Name = "Alheia", UnitPrice = 1, StockQuantity = 2 };
        db.Flowers.Add(flower);
        db.StockMovements.Add(new StockMovement
        {
            TenantId = other,
            FlowerId = flower.Id,
            Delta = 2,
            BalanceAfter = 2,
            Reason = StockMovementReason.Adjustment
        });
        await db.SaveChangesAsync();

        var stock = StockFor(opened, new StockGate(), new FakeEmailQueue(), new ListPushQueue());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => stock.ListMovementsAsync(flower.Id));
        Assert.Empty(await stock.ListLowStockAsync());
    }

    private static async Task<Fixture> SeedAsync(int stock, int minimum)
    {
        var opened = NewDb(Guid.NewGuid().ToString());
        var emails = new FakeEmailQueue();
        var push = new ListPushQueue();
        var flower = new Flower
        {
            TenantId = opened.Tenant.TenantId!.Value,
            Name = "Rosa",
            UnitPrice = 2,
            StockQuantity = stock,
            MinimumStockQuantity = minimum
        };
        opened.Db.Flowers.Add(flower);
        opened.Db.Tenants.Single().ContactEmail = "florista@loja.pt";
        await opened.Db.SaveChangesAsync();
        return new Fixture(opened, flower.Id, StockFor(opened, new StockGate(), emails, push), emails, push);
    }

    private static StockService StockFor(OpenedDb opened, StockGate gate, FakeEmailQueue emails, ListPushQueue push) =>
        new(new UnitOfWork(opened.Db), opened.Db, opened.Tenant, emails, push, gate);

    private static Order Order(Guid tenantId, Guid flowerId, int quantity) => new()
    {
        TenantId = tenantId,
        CustomerId = Guid.NewGuid(),
        Status = OrderStatus.PendingConfirmation,
        Items =
        {
            new OrderItem { TenantId = tenantId, FlowerId = flowerId, Name = "Rosa", Quantity = quantity, UnitPrice = 2 }
        }
    };

    private static OpenedDb NewDb(string name)
    {
        var tenant = new TenantContext();
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa11");
        tenant.SetTenant(tenantId, "loja");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
        var db = new ApplicationDbContext(options, tenant);
        if (!db.Tenants.Any())
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Loja", Slug = "loja", ContactEmail = "florista@loja.pt" });
        db.SaveChanges();
        return new OpenedDb(db, tenant);
    }

    private sealed record OpenedDb(ApplicationDbContext Db, TenantContext Tenant);

    private sealed class Fixture
    {
        private readonly OpenedDb _opened;
        public Fixture(OpenedDb opened, Guid flowerId, StockService stock, FakeEmailQueue emails, ListPushQueue push)
        {
            _opened = opened;
            FlowerId = flowerId;
            Stock = stock;
            Emails = emails;
            Push = push;
        }

        public ApplicationDbContext Db => _opened.Db;
        public Guid FlowerId { get; }
        public StockService Stock { get; }
        public FakeEmailQueue Emails { get; }
        public ListPushQueue Push { get; }

        public async Task<Order> OrderAsync(int quantity)
        {
            var order = Order(_opened.Tenant.TenantId!.Value, FlowerId, quantity);
            Db.Orders.Add(order);
            await Db.SaveChangesAsync();
            return order;
        }

        public async Task ConfirmAsync(Guid orderId)
        {
            var order = await ReloadAsync(orderId);
            await Stock.WithFlowerLocksAsync(order, async ct =>
            {
                await Stock.ApplyConfirmAsync(order, ct);
                await Db.SaveChangesAsync(ct);
            });
        }

        public Task<Order> ReloadAsync(Guid id) =>
            Db.Orders.Include(o => o.Items).SingleAsync(o => o.Id == id);

        public async Task<int> QuantityAsync() =>
            (await Db.Flowers.SingleAsync(f => f.Id == FlowerId)).StockQuantity!.Value;
    }
}

internal sealed class ListPushQueue : IPushQueue
{
    public List<PushWorkItem> Items { get; } = [];
    public void Enqueue(PushWorkItem item) => Items.Add(item);
}
