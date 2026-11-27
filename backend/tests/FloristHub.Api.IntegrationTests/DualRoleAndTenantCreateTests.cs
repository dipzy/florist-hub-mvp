using System.Net;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Seed;

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

        var foreignLogin = await authClient.SendJsonAsync(HttpMethod.Post, "/api/auth/login", new
        {
            email = DevelopmentDataSeeder.SuperAdminEmail,
            password = ApiTestClient.Demo.Password,
            tenantId = ApiTestClient.Demo.TenantCampo,
        });
        Assert.True(
            foreignLogin.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Login estrangeiro devia falhar, obteve {(int)foreignLogin.StatusCode}");

        var platformAgain = await authClient.LoginAuthAsync(DevelopmentDataSeeder.SuperAdminEmail, ApiTestClient.Demo.Password);
        Assert.Equal(Roles.SuperAdmin, platformAgain.Role);
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
