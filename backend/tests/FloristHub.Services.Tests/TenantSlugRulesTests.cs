using FloristHub.Services.Tenancy;

namespace FloristHub.Services.Tests;

public class TenantSlugRulesTests
{
    [Fact]
    public void Rejects_duplicate_slug_pattern_violations()
    {
        Assert.Throws<InvalidOperationException>(() => TenantSlugRules.EnsureValidSlug("-invalid"));
        Assert.Throws<InvalidOperationException>(() => TenantSlugRules.EnsureValidSlug("www"));
    }

    [Fact]
    public void Accepts_valid_slug()
    {
        var exception = Record.Exception(() => TenantSlugRules.EnsureValidSlug("flores-lina"));
        Assert.Null(exception);
    }
}
