using System.Net;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Seed;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class FloristOnboardingAuthorizationTests
{
    private readonly FloristHubPostgresFixture _fixture;

    public FloristOnboardingAuthorizationTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Florist_can_read_onboarding_status_for_own_tenant()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(
            Roles.Florist,
            ApiTestClient.Demo.FloristLinaUser,
            ApiTestClient.Demo.TenantLina,
            ApiTestClient.Demo.FloristLinaEmail);
        client.UseTenantHeaders(ApiTestClient.Demo.TenantLina, DevelopmentDataSeeder.DemoTenantSlug);

        var res = await client.GetAsync("/api/florist/onboarding");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await client.ReadJsonAsync<OnboardingJson>(res);
        Assert.NotNull(json);
        Assert.True(json!.IsPublished);
    }

    [Fact]
    public async Task Customer_cannot_access_florist_onboarding()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(
            Roles.Customer,
            ApiTestClient.Demo.CustomerUser,
            ApiTestClient.Demo.TenantLina,
            ApiTestClient.Demo.CustomerEmail);
        client.UseTenantHeaders(ApiTestClient.Demo.TenantLina, DevelopmentDataSeeder.DemoTenantSlug);

        var res = await client.GetAsync("/api/florist/onboarding");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private sealed class OnboardingJson
    {
        public bool IsPublished { get; set; }
        public bool CanPublish { get; set; }
    }
}
