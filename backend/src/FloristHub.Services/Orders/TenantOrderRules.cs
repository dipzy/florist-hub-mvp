using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.Services.DTOs.Orders;
using FloristHub.Services.Onboarding;

namespace FloristHub.Services.Orders;

public static class TenantOrderRules
{
    private static readonly string[] DayNamesPt =
    [
        "domingo", "segunda-feira", "terça-feira", "quarta-feira", "quinta-feira", "sexta-feira", "sábado"
    ];

    public static void EnsureCanAcceptOrder(Tenant tenant, CreateOrderRequest request)
    {
        if (!tenant.IsActive)
            throw new InvalidOperationException("Esta loja está suspensa e não aceita encomendas.");

        TenantPublicAccessRules.EnsurePublicStoreOpen(tenant);

        if (!tenant.OrdersAcceptingEnabled)
            throw new InvalidOperationException("Esta loja não está a aceitar encomendas de momento.");

        switch (request.DeliveryType)
        {
            case DeliveryType.StorePickup when !tenant.PickupEnabled:
                throw new InvalidOperationException("O levantamento na loja não está disponível.");
            case DeliveryType.HomeDelivery when !tenant.HomeDeliveryEnabled:
                throw new InvalidOperationException("A entrega ao domicílio não está disponível.");
            case DeliveryType.CemeteryOrOther when !tenant.CemeteryDeliveryEnabled:
                throw new InvalidOperationException("Este tipo de entrega não está disponível.");
        }

        EnsureDeliveryZoneSelected(tenant, request);

        if (request.RequestedDeliveryAt is { } deliveryAt)
        {
            var minWhen = DateTimeOffset.UtcNow.AddMinutes(tenant.MinimumOrderAdvanceMinutes);
            if (deliveryAt < minWhen)
                throw new InvalidOperationException(
                    $"É necessário antecedência mínima de {tenant.MinimumOrderAdvanceMinutes} minutos para a data pretendida.");

            EnsureWithinOpeningHours(tenant, deliveryAt);
        }
    }

    public static void EnsureDeliveryZoneSelected(Tenant tenant, CreateOrderRequest request)
    {
        if (request.DeliveryType is not (DeliveryType.HomeDelivery or DeliveryType.CemeteryOrOther))
            return;

        var hasActiveZones = tenant.DeliveryZones.Any(z => z.IsActive);
        if (hasActiveZones && request.DeliveryZoneId is null)
            throw new InvalidOperationException("Selecione uma zona de entrega.");
    }

    public static void EnsureWithinOpeningHours(Tenant tenant, DateTimeOffset requestedAt)
    {
        if (tenant.OpeningHours.Count == 0)
            return;

        var day = requestedAt.DayOfWeek;
        var dayConfig = tenant.OpeningHours.FirstOrDefault(h => h.DayOfWeek == day);
        if (dayConfig is null)
            return;

        if (dayConfig.IsClosed)
            throw new InvalidOperationException($"A loja está encerrada à {DayNamesPt[(int)day]}.");

        if (dayConfig.OpenTime is null || dayConfig.CloseTime is null)
            return;

        var time = TimeOnly.FromTimeSpan(requestedAt.TimeOfDay);
        if (time < dayConfig.OpenTime.Value || time > dayConfig.CloseTime.Value)
        {
            throw new InvalidOperationException(
                $"A data pretendida tem de ser dentro do horário de funcionamento " +
                $"({dayConfig.OpenTime.Value:HH:mm}–{dayConfig.CloseTime.Value:HH:mm}).");
        }
    }

    public static decimal ResolveDeliveryFee(Tenant tenant, CreateOrderRequest request, DeliveryZone? zone)
    {
        if (request.DeliveryType == DeliveryType.StorePickup)
            return 0m;

        if (zone is not null && zone.IsActive)
            return zone.Fee;

        return 0m;
    }
}
