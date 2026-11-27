using System.Net;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Seed;
using FloristHub.Services.DTOs.Auth;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class DualRoleAndTenantCreateTests
{
    private static readonly Guid SuperAdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222201");

    private readonly FloristHubPostgresFixture _fixture;

    public DualRoleAndTenantCreateTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    private ApiTestClient SuperAdminClient()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.SuperAdmin, SuperAdminUserId, Guid.Empty, DevelopmentDataSeeder.SuperAdminEmail);
        return client;
    }

    [Fact]
    public async Task Create_tenant_with_existing_superadmin_email_attaches_without_invite()
    {
        var slug = "sa-own-" + Guid.NewGuid().ToString("N")[..8];
        var admin = SuperAdminClient();

        var created = await admin.SendJsonAsync(HttpMethod.Post, "/api/platform/tenants", new
        {
            name = "Loja SuperAdmin",
            slug,
            floristEmail = DevelopmentDataSeeder.SuperAdminEmail,
            floristFullName = "Super Admin Demo",
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var body = await admin.ReadJsonAsync<ApiTestClient.CreateTenantBody>(created);
        Assert.NotNull(body);
        Assert.False(body!.FloristInviteSent);
        Assert.Null(body.DevInviteLink);

        var loginClient = new ApiTestClient(_fixture.Factory.CreateClient());
        var floristLogin = await loginClient.LoginAuthAsync(
            DevelopmentDataSeeder.SuperAdminEmail,
            ApiTestClient.Demo.Password,
            body.Tenant.Id);
        Assert.Equal(Roles.SuperAdmin, floristLogin.Role);
        Assert.NotNull(floristLogin.AvailableContexts);
        Assert.True(floristLogin.AvailableContexts!.Count >= 2);
    }

    [Fact]
    public async Task Create_tenant_with_new_email_still_sends_invite()
    {
        var slug = "new-fl-" + Guid.NewGuid().ToString("N")[..8];
        var email = $"florist-{Guid.NewGuid():N}@demo.floristhub.pt";

        var admin = SuperAdminClient();
        var created = await admin.SendJsonAsync(HttpMethod.Post, "/api/platform/tenants", new
        {
            name = "Florista Nova",
            slug,
            floristEmail = email,
            floristFullName = "Florista Nova",
        });
        created.EnsureSuccessStatusCode();
        var body = await admin.ReadJsonAsync<ApiTestClient.CreateTenantBody>(created);
        Assert.NotNull(body);
        Assert.True(body!.FloristInviteSent);
    }

    [Fact]
    public async Task Login_and_switch_context_respect_membership()
    {
        var slug = "sw-" + Guid.NewGuid().ToString("N")[..8];
        var admin = SuperAdminClient();
        var created = await admin.SendJsonAsync(HttpMethod.Post, "/api/platform/tenants", new
        {
            name = "Loja Switch",
            slug,
            floristEmail = DevelopmentDataSeeder.SuperAdminEmail,
            floristFullName = "Super Admin Demo",
        });
        created.EnsureSuccessStatusCode();
        var tenant = (await admin.ReadJsonAsync<ApiTestClient.CreateTenantBody>(created))!.Tenant;

        var authClient = new ApiTestClient(_fixture.Factory.CreateClient());
        var platform = await authClient.LoginAuthAsync(DevelopmentDataSeeder.SuperAdminEmail, ApiTestClient.Demo.Password);
        Assert.Equal(Roles.SuperAdmin, platform.Role);

        var storeLogin = await authClient.LoginAuthAsync(
            DevelopmentDataSeeder.SuperAdminEmail,
            ApiTestClient.Demo.Password,
            tenant.Id);
        Assert.Equal(Roles.SuperAdmin, storeLogin.Role);

        // A SuperAdmin always signs in to the platform context (ac8e48d), even from another store's page: the
        // token carries no store, so it gives no access to that store.
        var foreignLogin = await authClient.LoginAuthAsync(
            DevelopmentDataSeeder.SuperAdminEmail,
            ApiTestClient.Demo.Password,
            ApiTestClient.Demo.TenantCampo);
        Assert.Equal(Roles.SuperAdmin, foreignLogin.Role);
        Assert.Null(foreignLogin.TenantId);

        // Membership is what decides the store context: the own store is allowed, a foreign one is not.
        var session = new ApiTestClient(_fixture.Factory.CreateClient());
        session.UseBearerToken(foreignLogin.Token);
        var ownStore = await session.SendJsonAsync(HttpMethod.Post, "/api/auth/switch-context", new { kind = AuthContextKinds.Florist, tenantId = tenant.Id });
        Assert.Equal(HttpStatusCode.OK, ownStore.StatusCode);
        var foreignStore = await session.SendJsonAsync(
            HttpMethod.Post,
            "/api/auth/switch-context",
            new { kind = AuthContextKinds.Florist, tenantId = ApiTestClient.Demo.TenantCampo });
        Assert.True(
            foreignStore.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Mudar para uma loja sem pertença devia falhar, obteve {(int)foreignStore.StatusCode}");

        // Pointing the platform token at the foreign store gives no access to its florist panel.
        var platformSession = new ApiTestClient(_fixture.Factory.CreateClient());
        platformSession.UseBearerToken(foreignLogin.Token);
        platformSession.UseTenantHeaders(ApiTestClient.Demo.TenantCampo);
        Assert.Equal(HttpStatusCode.Forbidden, (await platformSession.GetAsync("/api/florist/reviews")).StatusCode);

        var platformAgain = await authClient.LoginAuthAsync(DevelopmentDataSeeder.SuperAdminEmail, ApiTestClient.Demo.Password);
        Assert.Equal(Roles.SuperAdmin, platformAgain.Role);
    }

    [Theory]
    [InlineData("florist")]
    [InlineData("customer")]
    public async Task Florists_and_customers_cannot_sign_in_to_a_store_they_do_not_belong_to(string who)
    {
        var email = who == "florist" ? ApiTestClient.Demo.FloristLinaEmail : ApiTestClient.Demo.CustomerEmail;
        var client = new ApiTestClient(_fixture.Factory.CreateClient());

        var foreign = await client.SendJsonAsync(HttpMethod.Post, "/api/auth/login", new
        {
            email,
            password = ApiTestClient.Demo.Password,
            tenantId = ApiTestClient.Demo.TenantCampo,
        });

        Assert.True(
            foreign.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Login numa loja sem pertença devia falhar, obteve {(int)foreign.StatusCode}");
        // The same account signs in to its own store.
        Assert.NotNull(await client.LoginAuthAsync(email, ApiTestClient.Demo.Password, ApiTestClient.Demo.TenantLina));
    }

    [Fact]
    public async Task Florist_only_login_unchanged()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        var login = await client.LoginAuthAsync(
            ApiTestClient.Demo.FloristLinaEmail,
            ApiTestClient.Demo.Password,
            ApiTestClient.Demo.TenantLina);
        Assert.Equal(Roles.Florist, login.Role);
        Assert.True(login.AvailableContexts is null or { Count: <= 1 });
    }

    [Fact]
    public async Task Customer_login_unchanged()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        var login = await client.LoginAuthAsync(
            ApiTestClient.Demo.CustomerEmail,
            ApiTestClient.Demo.Password,
            ApiTestClient.Demo.TenantLina);
        Assert.Equal(Roles.Customer, login.Role);
    }
}
