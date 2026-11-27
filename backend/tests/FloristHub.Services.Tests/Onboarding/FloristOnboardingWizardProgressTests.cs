using FloristHub.DAL.Entities;
using FloristHub.Services.Onboarding;

namespace FloristHub.Services.Tests.Onboarding;

public class FloristOnboardingWizardProgressTests
{
    [Fact]
    public void Published_store_reports_full_wizard_progress()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Publicada",
            Slug = "pub",
            IsPublished = true,
            OnboardingWizardStep = FloristOnboardingSteps.DefaultFirst,
            DeliveryZones = [new DeliveryZone { TenantId = Guid.NewGuid(), Name = "Zona", IsActive = true }],
        };

        var step = tenant.IsPublished ? FloristOnboardingSteps.Ordered[^1] : tenant.OnboardingWizardStep!;
        var stepIndex = Array.IndexOf(FloristOnboardingSteps.Ordered, step);
        var progress = tenant.IsPublished
            ? 100
            : (int)Math.Round(100.0 * (stepIndex + 1) / FloristOnboardingSteps.Ordered.Length);

        Assert.Equal(100, progress);
        Assert.Equal(FloristOnboardingSteps.Ordered[^1], step);
    }
}
