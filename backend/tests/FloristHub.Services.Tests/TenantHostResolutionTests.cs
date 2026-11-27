using FloristHub.Services.Tenancy;

namespace FloristHub.Services.Tests;

public class TenantHostResolutionTests
{
    [Theory]
        [InlineData("flores-lina.localhost", null, false, "")]
        [InlineData("florist-hub-26m.pages.dev", null, false, "")]
        [InlineData("florist-hub-26m.pages.dev", "pages.dev", false, "")]
        [InlineData("florist-hub-api.onrender.com", null, false, "")]
        [InlineData("localhost", "floristhub.pt", false, "")]
        [InlineData("flores-lina.floristhub.pt", "floristhub.pt", true, "flores-lina")]
        [InlineData("floristhub.pt", "floristhub.pt", false, "")]
        [InlineData("www.floristhub.pt", "floristhub.pt", false, "")]
    public void Parses_subdomain_slug(string host, string? baseDomain, bool expected, string expectedSlug)
    {
        var ok = TenantHostResolution.TryParseSubdomainSlug(host, baseDomain, out var slug);
        Assert.Equal(expected, ok);
        if (expected)
            Assert.Equal(expectedSlug, slug);
    }

    [Theory]
        [InlineData("minhaloja.com", "floristhub.pt", true)]
        [InlineData("flores-lina.floristhub.pt", "floristhub.pt", false)]
        [InlineData("flores-lina.localhost", null, false)]
        [InlineData("florist-hub-26m.pages.dev", null, false)]
        [InlineData("florist-hub-api.onrender.com", null, false)]
    public void Detects_custom_domain_host(string host, string? baseDomain, bool expected) =>
        Assert.Equal(expected, TenantHostResolution.IsCustomDomainHost(host, baseDomain));
}
