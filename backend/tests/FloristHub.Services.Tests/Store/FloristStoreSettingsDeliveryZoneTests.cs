using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services;
using FloristHub.Services.DTOs.Store;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Tests.Store;

public class FloristStoreSettingsDeliveryZoneTests
{
    private static readonly Guid TenantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Ef_can_add_delivery_zone_to_tracked_tenant()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(TenantId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options, tenantContext);

        var existingZoneId = Guid.NewGuid();
        db.Tenants.Add(new Tenant
        {
            Id = TenantId,
            Name = "Teste",
            Slug = "teste",
            ThemePrimaryColor = "#2d6a4f",
            ThemeAccentColor = "#95d5b2",
            DeliveryZones =
            [
                new DeliveryZone
                {
                    Id = existingZoneId,
                    TenantId = TenantId,
                    Name = "Centro",
                    Fee = 5m,
                    IsActive = true,
                },
            ],
        });
        await db.SaveChangesAsync();

        var tenant = await db.Tenants
            .Include(t => t.DeliveryZones)
            .FirstAsync(t => t.Id == TenantId);

        await db.DeliveryZones.AddAsync(new DeliveryZone
        {
            TenantId = TenantId,
            Name = "Margem Sul",
            Fee = 8m,
            IsActive = true,
        });

        await db.SaveChangesAsync();
        await db.Entry(tenant).Collection(t => t.DeliveryZones).LoadAsync();

        Assert.Equal(2, tenant.DeliveryZones.Count);
    }

    [Fact]
    public async Task UpdateAsync_adds_new_delivery_zone_without_id()
    {
        await using var fixture = await CreateFixtureAsync();
        var existingZoneId = fixture.ExistingZoneId;

        var request = BuildRequest(fixture.Tenant, zones:
        [
            new DeliveryZoneInputDto(existingZoneId, "Centro", 5m, true, 0),
            new DeliveryZoneInputDto(null, "Margem Sul", 8m, true, 1),
        ]);

        var result = await fixture.Service.UpdateAsync(request, Guid.NewGuid());

        Assert.Equal(2, result.DeliveryZones.Count);
        Assert.Contains(result.DeliveryZones, z => z.Name == "Margem Sul" && z.Fee == 8m);
    }

    [Fact]
    public async Task UpdateAsync_adds_new_delivery_zone_with_untracked_client_guid()
    {
        await using var fixture = await CreateFixtureAsync();
        var clientNewId = Guid.NewGuid();

        var request = BuildRequest(fixture.Tenant, zones:
        [
            new DeliveryZoneInputDto(fixture.ExistingZoneId, "Centro", 5m, true, 0),
            new DeliveryZoneInputDto(clientNewId, "Norte", 12m, true, 1),
        ]);

        var result = await fixture.Service.UpdateAsync(request, Guid.NewGuid());

        Assert.Equal(2, result.DeliveryZones.Count);
        var norte = result.DeliveryZones.Single(z => z.Name == "Norte");
        Assert.Equal(clientNewId, norte.Id);
    }

    [Fact]
    public async Task UpdateAsync_updates_and_removes_delivery_zones()
    {
        await using var fixture = await CreateFixtureAsync();

        var request = BuildRequest(fixture.Tenant, zones:
        [
            new DeliveryZoneInputDto(fixture.ExistingZoneId, "Centro actualizado", 6m, true, 0),
        ]);

        var result = await fixture.Service.UpdateAsync(request, Guid.NewGuid());

        Assert.Single(result.DeliveryZones);
        Assert.Equal("Centro actualizado", result.DeliveryZones[0].Name);
        Assert.Equal(6m, result.DeliveryZones[0].Fee);
    }

    private static async Task<Fixture> CreateFixtureAsync()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(TenantId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options, tenantContext);

        var existingZoneId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = TenantId,
            Name = "Teste",
            Slug = "teste",
            ThemePrimaryColor = "#2d6a4f",
            ThemeAccentColor = "#95d5b2",
            DeliveryZones =
            [
                new DeliveryZone
                {
                    Id = existingZoneId,
                    TenantId = TenantId,
                    Name = "Centro",
                    Fee = 5m,
                    IsActive = true,
                    SortOrder = 0,
                },
            ],
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var service = new FloristStoreSettingsService(new UnitOfWork(db), tenantContext, new NullAudit());
        return new Fixture(db, service, tenant, existingZoneId);
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

    private static UpdateStoreSettingsRequest BuildRequest(Tenant t, IReadOnlyList<DeliveryZoneInputDto> zones) =>
        new(
            t.Name,
            t.Description,
            t.LogoUrl,
            t.ContactEmail,
            t.ContactPhone,
            t.AddressLine,
            t.City,
            t.PostalCode,
            t.ThemePrimaryColor,
            t.ThemeAccentColor,
            t.ThemeFontFamily,
            t.OrdersAcceptingEnabled,
            t.MinimumOrderAdvanceMinutes,
            t.PickupEnabled,
            t.HomeDeliveryEnabled,
            t.CemeteryDeliveryEnabled,
            Array.Empty<OpeningHourDto>(),
            zones);

    private sealed class Fixture : IAsyncDisposable
    {
        public Fixture(ApplicationDbContext db, FloristStoreSettingsService service, Tenant tenant, Guid existingZoneId)
        {
            Db = db;
            Service = service;
            Tenant = tenant;
            ExistingZoneId = existingZoneId;
        }

        public ApplicationDbContext Db { get; }
        public FloristStoreSettingsService Service { get; }
        public Tenant Tenant { get; }
        public Guid ExistingZoneId { get; }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
