using System.Net;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Seed;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class TenantDirectoryAndLoginTests
{
    private readonly FloristHubPostgresFixture _fixture;

    public TenantDirectoryAndLoginTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Public_tenant_list_is_not_exposed()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        var res = await client.GetAsync("/api/tenants");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Resolve_by_slug_returns_single_store_without_directory()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        var res = await client.GetAsync("/api/tenants/resolve/lina");
        res.EnsureSuccessStatusCode();
        var body = await client.ReadJsonAsync<ResolveBody>(res);
        Assert.NotNull(body);
        Assert.Equal(ApiTestClient.Demo.TenantLina, body!.Id);
        Assert.False(string.IsNullOrWhiteSpace(body.Slug));

        var missing = await client.GetAsync("/api/tenants/resolve/loja-inexistente-xyz");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_login_with_store_context_defaults_to_platform_role()
    {
        var slug = "sa-def-" + Guid.NewGuid().ToString("N")[..8];
        var admin = new ApiTestClient(_fixture.Factory.CreateClient());
        admin.UseTestPrincipal(Roles.SuperAdmin, Guid.Parse("22222222-2222-2222-2222-222222222201"), Guid.Empty, DevelopmentDataSeeder.SuperAdminEmail);

        var created = await admin.SendJsonAsync(HttpMethod.Post, "/api/platform/tenants", new
        {
            name = "Loja SA default",
            slug,
            floristEmail = DevelopmentDataSeeder.SuperAdminEmail,
            floristFullName = "Super Admin Demo",
        });
        created.EnsureSuccessStatusCode();
        var tenant = (await admin.ReadJsonAsync<ApiTestClient.CreateTenantBody>(created))!.Tenant;

        var loginClient = new ApiTestClient(_fixture.Factory.CreateClient());
        var login = await loginClient.LoginAuthAsync(
            DevelopmentDataSeeder.SuperAdminEmail,
            ApiTestClient.Demo.Password,
            tenant.Id);
        Assert.Equal(Roles.SuperAdmin, login.Role);
    }

    [Fact]
    public async Task Login_without_tenant_still_works_for_SuperAdmin_on_platform()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        var login = await client.LoginAuthAsync(DevelopmentDataSeeder.SuperAdminEmail, ApiTestClient.Demo.Password);
        Assert.Equal(Roles.SuperAdmin, login.Role);
    }

    [Fact]
    public async Task Bad_password_returns_generic_login_message()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        var res = await client.SendJsonAsync(HttpMethod.Post, "/api/auth/login", new
        {
            email = DevelopmentDataSeeder.SuperAdminEmail,
            password = "wrong-password-xyz",
        });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        var json = await res.Content.ReadAsStringAsync();
        Assert.Contains("Email ou palavra-passe incorretos", json, StringComparison.Ordinal);
    }

    private sealed record ResolveBody(Guid Id, string Name, string Slug, string? Description);
}
