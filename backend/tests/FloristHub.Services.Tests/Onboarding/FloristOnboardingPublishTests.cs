using FloristHub.DAL.Entities;
using FloristHub.Services.Onboarding;

namespace FloristHub.Services.Tests.Onboarding;

public class FloristOnboardingPublishTests
{
    [Fact]
    public void Publish_requires_shop_data_colors_flower_zone_terms_and_payments()
    {
        var tenant = BuildTenant();
        var missing = TenantPublicationRules.MissingRequiredForPublish(tenant, hasFlower: false, stripeConfigured: false);
        Assert.Contains("dados-loja", missing);
        Assert.Contains("cores", missing);
        Assert.Contains("catalogo", missing);
        Assert.Contains("pagamentos", missing);
        Assert.Contains("termos-plataforma", missing);
    }

    [Fact]
    public void Payments_disabled_by_choice_satisfies_payment_requirement_without_stripe()
    {
        var tenant = BuildTenant();
        tenant.PaymentsDisabledByChoice = true;
        Assert.True(TenantPublicationRules.PaymentsRequirementMet(tenant, stripeConfigured: false));
    }

    private static Tenant BuildTenant() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Loja",
        Slug = "loja",
        DeliveryZones = [new DeliveryZone { TenantId = Guid.NewGuid(), Name = "Zona", IsActive = true }],
    };
}
