using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Onboarding;

public static class TenantPublicationRules
{
    public static bool HasShopBasics(Tenant tenant) =>
        !string.IsNullOrWhiteSpace(tenant.Name)
        && !string.IsNullOrWhiteSpace(tenant.ContactEmail)
        && !string.IsNullOrWhiteSpace(tenant.ContactPhone)
        && !string.IsNullOrWhiteSpace(tenant.AddressLine)
        && !string.IsNullOrWhiteSpace(tenant.City)
        && !string.IsNullOrWhiteSpace(tenant.PostalCode);

    public static bool HasColors(Tenant tenant) => tenant.OnboardingColorsAcknowledged;

    public static bool HasActiveDeliveryZone(Tenant tenant) =>
        tenant.DeliveryZones.Any(z => z.IsActive);

    public static bool HasTerms(Tenant tenant) => tenant.PlatformTermsAcceptedAt is not null;

    public static bool PaymentsRequirementMet(Tenant tenant, bool stripeConfigured) =>
        tenant.PaymentsDisabledByChoice
        || (stripeConfigured
            && !string.IsNullOrWhiteSpace(tenant.StripeAccountId)
            && tenant.StripeChargesEnabled);

    public static async Task<bool> HasCatalogFlowerAsync(
        IQueryable<Flower> flowers,
        Guid tenantId,
        CancellationToken ct)
    {
        return await flowers
            .IgnoreQueryFilters()
            .Where(f => f.TenantId == tenantId && f.IsCatalogActive && f.StockQuantity > 0)
            .AnyAsync(ct);
    }

    public static IReadOnlyList<string> MissingRequiredForPublish(
        Tenant tenant,
        bool hasFlower,
        bool stripeConfigured)
    {
        var missing = new List<string>();
        if (!HasShopBasics(tenant))
            missing.Add("dados-loja");
        if (!HasColors(tenant))
            missing.Add("cores");
        if (!hasFlower)
            missing.Add("catalogo");
        if (!HasActiveDeliveryZone(tenant))
            missing.Add("zona-entrega");
        if (!HasTerms(tenant))
            missing.Add("termos-plataforma");
        if (!PaymentsRequirementMet(tenant, stripeConfigured))
            missing.Add("pagamentos");
        return missing;
    }
}
