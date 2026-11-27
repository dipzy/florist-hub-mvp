using System.Net;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Seed;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class PlatformMetricsAuthorizationTests
{
    private static readonly Guid SuperAdminUser = Guid.Parse("22222222-2222-2222-2222-222222222201");
    private readonly FloristHubPostgresFixture _fixture;

    public PlatformMetricsAuthorizationTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SuperAdmin_can_read_platform_metrics()
    {
        var client = Client(Roles.SuperAdmin, SuperAdminUser, Guid.Empty, DevelopmentDataSeeder.SuperAdminEmail);
        var response = await client.GetAsync("/api/platform/metrics?days=30");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Florist_cannot_read_platform_metrics()
    {
        var client = Client(Roles.Florist, ApiTestClient.Demo.FloristLinaUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.FloristLinaEmail);
        var response = await client.GetAsync("/api/platform/metrics?days=30");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Customer_cannot_read_platform_metrics()
    {
        var client = Client(Roles.Customer, ApiTestClient.Demo.CustomerUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.CustomerEmail);
        var response = await client.GetAsync("/api/platform/metrics?days=30");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private ApiTestClient Client(string role, Guid userId, Guid tenantId, string email)
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(role, userId, tenantId, email);
        return client;
    }
}
