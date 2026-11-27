using System.Net;
using FloristHub.DAL.Constants;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class PushNotificationIsolationTests
{
    private readonly FloristHubPostgresFixture _fixture;

    public PushNotificationIsolationTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Device_and_preferences_are_isolated_per_tenant()
    {
        const string token = "integration-push-token-74c9";
        var lina = Client(Roles.Florist, ApiTestClient.Demo.FloristLinaUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.FloristLinaEmail);
        var campo = Client(Roles.Florist, ApiTestClient.Demo.FloristCampoUser, ApiTestClient.Demo.TenantCampo, ApiTestClient.Demo.FloristCampoEmail);

        var register = await lina.SendJsonAsync(HttpMethod.Post, "/api/notifications/devices", new { token, platform = "Android" });
        Assert.Equal(HttpStatusCode.NoContent, register.StatusCode);

        var foreignDelete = await campo.SendJsonAsync(HttpMethod.Delete, "/api/notifications/devices", new { token });
        Assert.Equal(HttpStatusCode.NotFound, foreignDelete.StatusCode);

        var move = await campo.SendJsonAsync(HttpMethod.Post, "/api/notifications/devices", new { token, platform = "Android" });
        Assert.Equal(HttpStatusCode.NoContent, move.StatusCode);

        var oldOwner = await lina.SendJsonAsync(HttpMethod.Delete, "/api/notifications/devices", new { token });
        Assert.Equal(HttpStatusCode.NotFound, oldOwner.StatusCode);

        var owner = await campo.SendJsonAsync(HttpMethod.Delete, "/api/notifications/devices", new { token });
        Assert.Equal(HttpStatusCode.NoContent, owner.StatusCode);

        var saved = await lina.SendJsonAsync(HttpMethod.Put, "/api/notifications/preferences", new { newOrder = false, orderPaid = true, orderStatus = true });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var linaPrefs = await lina.ReadJsonAsync<Prefs>(await lina.GetAsync("/api/notifications/preferences"));
        var campoPrefs = await campo.ReadJsonAsync<Prefs>(await campo.GetAsync("/api/notifications/preferences"));
        Assert.False(linaPrefs!.NewOrder);
        Assert.True(campoPrefs!.NewOrder);
    }

    [Fact]
    public async Task Rejects_unknown_platform()
    {
        var client = Client(Roles.Customer, ApiTestClient.Demo.CustomerUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.CustomerEmail);
        var response = await client.SendJsonAsync(HttpMethod.Post, "/api/notifications/devices", new { token = "token-cliente-valido", platform = "Windows" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private ApiTestClient Client(string role, Guid userId, Guid tenantId, string email)
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(role, userId, tenantId, email);
        return client;
    }

    private sealed record Prefs(bool NewOrder, bool OrderPaid, bool OrderStatus);
}
