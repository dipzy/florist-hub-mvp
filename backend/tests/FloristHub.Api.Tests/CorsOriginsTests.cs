using FloristHub.Api.Configuration;

namespace FloristHub.Api.Tests;

public class CorsOriginsTests
{
    [Theory]
    [InlineData("floristhub.pt", "floristhub.pt", true)]
    [InlineData("flores-lina.floristhub.pt", "floristhub.pt", true)]
    [InlineData("www.floristhub.pt", "floristhub.pt", true)]
    [InlineData("floristhub.pt.evil.test", "floristhub.pt", false)]
    [InlineData("exemplo.pt", "floristhub.pt", false)]
    public void Origem_da_plataforma_cobre_o_apex_e_os_subdominios(string host, string baseDomain, bool expected) =>
        Assert.Equal(expected, CorsOrigins.IsPlatformHost(host, baseDomain));
}
