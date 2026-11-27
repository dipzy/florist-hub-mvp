using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.Services.DTOs.Orders;
using FloristHub.Services.Orders;

namespace FloristHub.Services.Tests;

public class TenantOrderRulesTests
{
    [Fact]
    public void Rejects_orders_when_store_is_suspended()
    {
        var tenant = BuildTenant();
        tenant.IsActive = false;
        var request = BuildRequest(DeliveryType.StorePickup);

        var ex = Assert.Throws<InvalidOperationException>(() => TenantOrderRules.EnsureCanAcceptOrder(tenant, request));
        Assert.Contains("suspensa", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_orders_when_store_disabled()
    {
        var tenant = BuildTenant(ordersAccepting: false);
        var request = BuildRequest(DeliveryType.StorePickup);

        var ex = Assert.Throws<InvalidOperationException>(() => TenantOrderRules.EnsureCanAcceptOrder(tenant, request));
        Assert.Contains("não está a aceitar", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Enforces_minimum_advance_minutes()
    {
        var tenant = BuildTenant(minAdvance: 180);
        var request = BuildRequest(DeliveryType.HomeDelivery, DateTimeOffset.UtcNow.AddMinutes(30));

        Assert.Throws<InvalidOperationException>(() => TenantOrderRules.EnsureCanAcceptOrder(tenant, request));
    }

    [Fact]
    public void Resolves_delivery_fee_from_zone()
    {
        var tenant = BuildTenant();
        var zone = new DeliveryZone { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Lisboa", Fee = 4.5m, IsActive = true };
        tenant.DeliveryZones.Add(zone);
        var request = BuildRequest(DeliveryType.HomeDelivery, deliveryZoneId: zone.Id);

        var fee = TenantOrderRules.ResolveDeliveryFee(tenant, request, zone);
        Assert.Equal(4.5m, fee);
    }

    [Fact]
    public void Requires_delivery_zone_for_cemetery_when_zones_exist()
    {
        var tenant = BuildTenant();
        tenant.DeliveryZones.Add(new DeliveryZone
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Centro",
            Fee = 3m,
            IsActive = true,
        });
        var request = BuildRequest(DeliveryType.CemeteryOrOther);

        var ex = Assert.Throws<InvalidOperationException>(() => TenantOrderRules.EnsureCanAcceptOrder(tenant, request));
        Assert.Contains("zona de entrega", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cemetery_fee_uses_selected_zone_not_fallback()
    {
        var tenant = BuildTenant();
        var zoneA = new DeliveryZone
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Zona A",
            Fee = 5m,
            SortOrder = 0,
            IsActive = true,
        };
        var zoneB = new DeliveryZone
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Zona B",
            Fee = 9m,
            SortOrder = 1,
            IsActive = true,
        };
        tenant.DeliveryZones.Add(zoneA);
        tenant.DeliveryZones.Add(zoneB);
        var request = BuildRequest(DeliveryType.CemeteryOrOther, deliveryZoneId: zoneB.Id);

        var fee = TenantOrderRules.ResolveDeliveryFee(tenant, request, zoneB);
        Assert.Equal(9m, fee);
    }

    [Fact]
    public void Cemetery_without_zone_selected_has_zero_fee_when_no_fallback()
    {
        var tenant = BuildTenant();
        tenant.DeliveryZones.Add(new DeliveryZone
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Única",
            Fee = 4m,
            IsActive = true,
        });
        var request = BuildRequest(DeliveryType.CemeteryOrOther);

        var fee = TenantOrderRules.ResolveDeliveryFee(tenant, request, null);
        Assert.Equal(0m, fee);
    }

    [Fact]
    public void Requires_delivery_zone_for_home_delivery_when_zones_exist()
    {
        var tenant = BuildTenant();
        tenant.DeliveryZones.Add(new DeliveryZone
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Centro",
            Fee = 3m,
            IsActive = true,
        });
        var request = BuildRequest(DeliveryType.HomeDelivery);

        var ex = Assert.Throws<InvalidOperationException>(() => TenantOrderRules.EnsureCanAcceptOrder(tenant, request));
        Assert.Contains("zona de entrega", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_requested_time_when_store_closed_that_day()
    {
        var tenant = BuildTenant();
        var sunday = DateTimeOffset.UtcNow.AddDays(7);
        while (sunday.DayOfWeek != DayOfWeek.Sunday)
            sunday = sunday.AddDays(1);
        sunday = sunday.Date.AddHours(12);

        tenant.OpeningHours.Add(new TenantOpeningHour
        {
            TenantId = tenant.Id,
            DayOfWeek = DayOfWeek.Sunday,
            IsClosed = true,
        });

        var request = BuildRequest(DeliveryType.StorePickup, sunday);
        var ex = Assert.Throws<InvalidOperationException>(() => TenantOrderRules.EnsureCanAcceptOrder(tenant, request));
        Assert.Contains("encerrada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Tenant BuildTenant(bool ordersAccepting = true, int minAdvance = 120) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        Slug = "test",
        OrdersAcceptingEnabled = ordersAccepting,
        MinimumOrderAdvanceMinutes = minAdvance,
        PickupEnabled = true,
        HomeDeliveryEnabled = true,
        CemeteryDeliveryEnabled = true,
        IsPublished = true,
    };

    private static CreateOrderRequest BuildRequest(
        DeliveryType deliveryType,
        DateTimeOffset? at = null,
        Guid? deliveryZoneId = null) =>
        new(
            ArrangementBaseType.Sponge,
            deliveryType,
            "Rua A",
            at,
            null,
            null,
            deliveryZoneId,
            [new OrderItemRequest(null, "Rosa", 1, 2m)]);
}
