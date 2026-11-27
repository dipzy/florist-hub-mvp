using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Capacity;
using FloristHub.Services.DTOs.Capacity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FloristHub.Services.Tests.Capacity;

public class DeliveryCapacityServiceTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");

    [Fact]
    public async Task Daily_limit_rejects_the_order_that_exceeds_it_and_release_frees_the_slot()
    {
        var fx = await CreateAsync();
        await fx.Capacity.UpdateSettingsAsync(new UpdateDeliveryCapacitySettingsRequest(1, []), Guid.NewGuid());
        var when = Future();

        Assert.True(await ReserveAsync(fx, when));
        var denied = await Assert.ThrowsAsync<InvalidOperationException>(() => ReserveAsync(fx, when));
        Assert.Contains("vagas", denied.Message, StringComparison.OrdinalIgnoreCase);

        var held = await fx.Db.Orders.SingleAsync();
        await fx.Capacity.RunLockedAsync(TenantA, held.CapacityServiceDate, async ct =>
        {
            await fx.Capacity.ApplyReleaseAsync(held, ct);
            await fx.Db.SaveChangesAsync(ct);
        });

        Assert.False(held.CapacityHeld);
        Assert.True(await ReserveAsync(fx, when));
    }

    [Fact]
    public async Task Closed_day_is_rejected_and_peak_override_accepts_more_orders()
    {
        var fx = await CreateAsync();
        await fx.Capacity.UpdateSettingsAsync(new UpdateDeliveryCapacitySettingsRequest(1, []), Guid.NewGuid());
        var when = Future();
        var date = DateOnly.FromDateTime(when.DateTime);

        await fx.Capacity.UpsertExceptionAsync(new UpsertDeliveryCapacityExceptionRequest(date, true, null, "Feriado"), Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReserveAsync(fx, when));

        await fx.Capacity.UpsertExceptionAsync(new UpsertDeliveryCapacityExceptionRequest(date, false, 2, "Pico"), Guid.NewGuid());
        Assert.True(await ReserveAsync(fx, when));
        Assert.True(await ReserveAsync(fx, when));
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReserveAsync(fx, when));
    }

    [Fact]
    public async Task Window_limit_is_enforced_and_time_outside_windows_is_rejected()
    {
        var fx = await CreateAsync();
        await fx.Capacity.UpdateSettingsAsync(new UpdateDeliveryCapacitySettingsRequest(5,
        [
            new DeliveryWindowInput("Manhã", "09:00", "11:59", 1),
            new DeliveryWindowInput("Tarde", "12:00", "18:00", 2)
        ]), Guid.NewGuid());

        var morning = Future().Date + TimeSpan.FromHours(10);
        var whenMorning = new DateTimeOffset(morning, TimeSpan.Zero);
        var afternoon = new DateTimeOffset(morning.Date + TimeSpan.FromHours(15), TimeSpan.Zero);
        var night = new DateTimeOffset(morning.Date + TimeSpan.FromHours(20), TimeSpan.Zero);

        Assert.True(await ReserveAsync(fx, whenMorning));
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReserveAsync(fx, whenMorning));
        Assert.True(await ReserveAsync(fx, afternoon));
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReserveAsync(fx, night));
    }

    [Fact]
    public async Task Parallel_reservations_keep_only_one_order_on_the_last_slot()
    {
        var dbName = Guid.NewGuid().ToString();
        var gate = new CapacityGate();
        var seed = NewDb(dbName, TenantA);
        await using (seed.Db)
        {
            var fx = Factory(seed, gate);
            await fx.Capacity.UpdateSettingsAsync(new UpdateDeliveryCapacitySettingsRequest(1, []), Guid.NewGuid());
        }

        var when = Future();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            var db = NewDb(dbName, TenantA);
            await using var _ = db.Db;
            var fx = Factory(db, gate);
            try
            {
                return await ReserveAsync(fx, when);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        })));

        Assert.Equal(1, results.Count(ok => ok));
        var check = NewDb(dbName, TenantA);
        await using var checkDb = check.Db;
        var hold = await checkDb.DeliveryCapacityHolds.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(1, hold.BookedCount);
    }

    [Fact]
    public async Task Settings_and_calendar_do_not_leak_across_tenants()
    {
        var gate = new CapacityGate();
        var opened = NewDb(Guid.NewGuid().ToString(), TenantA);
        await using var db = opened.Db;
        db.Tenants.Add(new Tenant { Id = TenantB, Name = "B", Slug = "loja-b" });
        await db.SaveChangesAsync();
        var fx = Factory(opened, gate);
        await fx.Capacity.UpdateSettingsAsync(new UpdateDeliveryCapacitySettingsRequest(4, []), Guid.NewGuid());

        fx.Tenant.SetTenant(TenantB, "loja-b");
        var other = await fx.Capacity.GetSettingsAsync();
        Assert.Null(other.DailyOrderLimit);

        var from = DateOnly.FromDateTime(DateTime.UtcNow);
        var calendar = await fx.Capacity.GetFloristCalendarAsync(from, from.AddDays(1));
        Assert.All(calendar, day => Assert.Null(day.DailyLimit));
    }

    private static DateTimeOffset Future()
    {
        var day = DateTime.UtcNow.Date.AddDays(10);
        return new DateTimeOffset(day.AddHours(11), TimeSpan.Zero);
    }

    private static async Task<bool> ReserveAsync(Fixture fx, DateTimeOffset when)
    {
        var order = new Order
        {
            TenantId = fx.Tenant.TenantId!.Value,
            CustomerId = Guid.NewGuid(),
            RequestedDeliveryAt = when,
            DeliveryType = DeliveryType.StorePickup,
            BaseType = ArrangementBaseType.Jar
        };
        await fx.Capacity.RunLockedAsync(order.TenantId, CapacityRules.DateOf(when), async ct =>
        {
            var tenant = await fx.Db.Tenants.FirstAsync(t => t.Id == order.TenantId, ct);
            await fx.Capacity.ApplyReserveAsync(order, tenant, ct);
            fx.Db.Orders.Add(order);
            await fx.Db.SaveChangesAsync(ct);
        });
        return order.CapacityHeld;
    }

    private static async Task<Fixture> CreateAsync()
    {
        var opened = NewDb(Guid.NewGuid().ToString(), TenantA);
        return Factory(opened, new CapacityGate());
    }

    private static Fixture Factory(OpenedDb opened, CapacityGate gate)
    {
        var uow = new UnitOfWork(opened.Db);
        var audit = new AuditLogger(uow, NullLogger<AuditLogger>.Instance);
        var capacity = new DeliveryCapacityService(uow, opened.Db, opened.Tenant, audit, new RelationalWork(opened.Db), gate);
        return new Fixture(opened.Db, opened.Tenant, capacity);
    }

    private static OpenedDb NewDb(string name, Guid tenantId)
    {
        var tenant = new TenantContext();
        tenant.SetTenant(tenantId, "loja");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
        var db = new ApplicationDbContext(options, tenant);
        if (!db.Tenants.Any(t => t.Id == tenantId))
        {
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Loja", Slug = "loja-" + tenantId.ToString()[..8], ContactEmail = "florista@loja.pt" });
            db.SaveChanges();
        }

        return new OpenedDb(db, tenant);
    }

    private sealed record OpenedDb(ApplicationDbContext Db, TenantContext Tenant);

    private sealed record Fixture(ApplicationDbContext Db, TenantContext Tenant, DeliveryCapacityService Capacity);
}
