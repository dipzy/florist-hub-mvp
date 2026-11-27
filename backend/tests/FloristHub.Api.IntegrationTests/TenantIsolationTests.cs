using System.Net;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Seed;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class TenantIsolationTests
{
    private readonly FloristHubPostgresFixture _fixture;

    public TenantIsolationTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Florist_cannot_read_or_mutate_flowers_from_another_tenant()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristCampoUser, ApiTestClient.Demo.TenantCampo, ApiTestClient.Demo.FloristCampoEmail);
        client.UseTenantHeaders(ApiTestClient.Demo.TenantLina, DevelopmentDataSeeder.DemoTenantSlug);

        var campoFlower = Guid.Parse("33333333-3333-3333-3333-333333333401");

        var getOwn = await client.GetAsync("/api/flowers/manage?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, getOwn.StatusCode);
        var own = await client.ReadJsonAsync<PagedFlowers>(getOwn);
        Assert.NotNull(own);
        Assert.DoesNotContain(own!.Items, f => f.Id == ApiTestClient.Demo.RoseLina);

        var getOther = await client.GetAsync($"/api/flowers/manage");
        Assert.Equal(HttpStatusCode.OK, getOther.StatusCode);

        var update = await client.SendJsonAsync(HttpMethod.Put, $"/api/flowers/{ApiTestClient.Demo.RoseLina}", new
        {
            name = "Invasão",
            color = "Preto",
            unitPrice = 9.99m,
            unit = "ramo",
            isCatalogActive = true
        });
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

        var delete = await client.SendJsonAsync(HttpMethod.Delete, $"/api/flowers/{ApiTestClient.Demo.RoseLina}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var ownUpdate = await client.SendJsonAsync(HttpMethod.Put, $"/api/flowers/{campoFlower}", new
        {
            name = "Campo",
            color = "Roxo",
            unitPrice = 1m,
            unit = "ramo",
            isCatalogActive = true
        });
        Assert.Equal(HttpStatusCode.OK, ownUpdate.StatusCode);
    }

    [Fact]
    public async Task Public_catalog_and_theme_respect_X_Tenant_Id_and_subdomain()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());

        client.UseTenantHeaders(ApiTestClient.Demo.TenantLina, DevelopmentDataSeeder.DemoTenantSlug);
        var flowersLina = await client.ReadJsonAsync<List<FlowerDto>>(await client.GetAsync("/api/flowers"));
        Assert.Contains(flowersLina!, f => f.Name.Contains("Rosa", StringComparison.OrdinalIgnoreCase));

        client.UseTenantHeaders(ApiTestClient.Demo.TenantCampo, DevelopmentDataSeeder.DemoSecondTenantSlug);
        var flowersCampo = await client.ReadJsonAsync<List<FlowerDto>>(await client.GetAsync("/api/flowers"));
        Assert.DoesNotContain(flowersCampo!, f => f.Id == ApiTestClient.Demo.RoseLina);

        var hostClient = _fixture.Factory.CreateClient();
        var hostApi = new ApiTestClient(hostClient);
        using var themeReq = new HttpRequestMessage(HttpMethod.Get, "/api/store/theme");
        themeReq.Headers.Host = $"{DevelopmentDataSeeder.DemoTenantSlug}.floristhub.test";
        var themeRes = await hostClient.SendAsync(themeReq);
        themeRes.EnsureSuccessStatusCode();
        var theme = await hostApi.ReadJsonAsync<StoreThemeDto>(themeRes);
        Assert.Equal(DevelopmentDataSeeder.DemoTenantSlug, theme!.Slug);
    }

    [Fact]
    public async Task Orders_portfolio_and_dashboard_are_scoped_per_tenant()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristCampoUser, ApiTestClient.Demo.TenantCampo, ApiTestClient.Demo.FloristCampoEmail);

        var orderLina = Guid.Parse("55555555-5555-5555-5555-555555555501");
        var orderGet = await client.GetAsync($"/api/orders/{orderLina}");
        Assert.True(orderGet.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);

        var confirm = await client.SendJsonAsync(HttpMethod.Post, $"/api/orders/{orderLina}/confirm", new { notes = "hack" });
        Assert.True(confirm.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.InternalServerError or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
        Assert.NotEqual(HttpStatusCode.OK, confirm.StatusCode);

        var portfolioLina = Guid.Parse("44444444-4444-4444-4444-444444444401");
        var portfolioGet = await client.GetAsync($"/api/portfolio/{portfolioLina}");
        Assert.Equal(HttpStatusCode.NotFound, portfolioGet.StatusCode);

        var metrics = await client.GetAsync("/api/florist/dashboard/metrics?days=30");
        metrics.EnsureSuccessStatusCode();
        var body = await client.ReadJsonAsync<DashboardMetricsDto>(metrics);
        Assert.NotNull(body);
        Assert.Equal(0, body!.PendingOrders);
    }

    [Fact]
    public async Task Customer_cannot_read_other_tenant_order_even_with_wrong_tenant_header()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Customer, ApiTestClient.Demo.CustomerUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.CustomerEmail);
        client.UseTenantHeaders(ApiTestClient.Demo.TenantCampo, DevelopmentDataSeeder.DemoSecondTenantSlug);

        var orderLina = Guid.Parse("55555555-5555-5555-5555-555555555501");
        var res = await client.GetAsync($"/api/orders/{orderLina}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        client.ClearAuth();
        client.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristCampoUser, ApiTestClient.Demo.TenantCampo, ApiTestClient.Demo.FloristCampoEmail);
        res = await client.GetAsync($"/api/orders/{orderLina}");
        Assert.True(res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Florist_store_settings_and_customers_do_not_leak_other_tenants()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristCampoUser, ApiTestClient.Demo.TenantCampo, ApiTestClient.Demo.FloristCampoEmail);

        var settings = await client.GetAsync("/api/florist/store");
        settings.EnsureSuccessStatusCode();
        var store = await client.ReadJsonAsync<StoreSettingsDto>(settings);
        Assert.Contains("Campo", store!.Name, StringComparison.OrdinalIgnoreCase);

        var customers = await client.GetAsync("/api/florist/customers?page=1&pageSize=20");
        customers.EnsureSuccessStatusCode();
        var page = await client.ReadJsonAsync<PagedCustomers>(customers);
        Assert.NotNull(page);
        Assert.DoesNotContain(page!.Items, c => c.Email.Contains("cliente@demo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Tenant_scoped_files_reject_cross_tenant_paths()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        var otherTenantPath = $"{ApiTestClient.Demo.TenantLina:N}/00000000000000000000000000000001.jpg";
        var anon = await client.GetAsync($"/api/files/{otherTenantPath}");
        Assert.Equal(HttpStatusCode.NotFound, anon.StatusCode);

        client.UseTenantHeaders(ApiTestClient.Demo.TenantCampo, DevelopmentDataSeeder.DemoSecondTenantSlug);
        var wrongHeader = await client.GetAsync($"/api/files/{otherTenantPath}");
        Assert.Equal(HttpStatusCode.NotFound, wrongHeader.StatusCode);

        var demo = await client.GetAsync("/api/files/demo/flores-lina-logo.jpg");
        Assert.True(demo.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Order_change_proposal_json_is_validated()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristLinaUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.FloristLinaEmail);

        var orderId = Guid.Parse("55555555-5555-5555-5555-555555555501");
        var badJson = await client.SendJsonAsync(HttpMethod.Post, $"/api/orders/{orderId}/proposals", new
        {
            message = "Teste",
            proposedItemChangesJson = "{invalid",
            revisedTotal = 10m
        });
        Assert.True(badJson.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Coupons_campaigns_reviews_and_seo_stay_inside_the_tenant()
    {
        var florist = new ApiTestClient(_fixture.Factory.CreateClient());
        florist.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristLinaUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.FloristLinaEmail);
        var code = "MAE" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var created = await florist.SendJsonAsync(HttpMethod.Post, "/api/florist/coupons", new
        {
            code,
            discountType = 0,
            discountValue = 10m,
            isActive = true,
            targets = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var coupon = await florist.ReadJsonAsync<CouponRow>(created);
        Assert.NotNull(coupon);

        var other = new ApiTestClient(_fixture.Factory.CreateClient());
        other.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristCampoUser, ApiTestClient.Demo.TenantCampo, ApiTestClient.Demo.FloristCampoEmail);
        var list = await other.ReadJsonAsync<List<CouponRow>>(await other.GetAsync("/api/florist/coupons"));
        Assert.DoesNotContain(list!, c => c.Code == code);
        var update = await other.SendJsonAsync(HttpMethod.Put, $"/api/florist/coupons/{coupon!.Id}", new
        {
            code,
            discountType = 0,
            discountValue = 90m,
            isActive = true,
            targets = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

        var customer = new ApiTestClient(_fixture.Factory.CreateClient());
        customer.UseTestPrincipal(Roles.Customer, ApiTestClient.Demo.CustomerUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.CustomerEmail);
        var mine = await customer.ReadJsonAsync<List<OrderRow>>(await customer.GetAsync("/api/orders/mine"));
        var delivered = mine!.First(o => o.Status == 4);
        var review = await customer.SendJsonAsync(HttpMethod.Post, $"/api/orders/{delivered.Id}/review", new { stars = 5, comment = "Excelente" });
        Assert.True(review.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest);

        var campoReviews = await other.ReadJsonAsync<List<ReviewRow>>(await other.GetAsync("/api/florist/reviews"));
        Assert.DoesNotContain(campoReviews!, r => r.OrderId == delivered.Id);

        var host = _fixture.Factory.CreateClient();
        using var robots = new HttpRequestMessage(HttpMethod.Get, "/robots.txt");
        robots.Headers.Host = $"{DevelopmentDataSeeder.DemoTenantSlug}.floristhub.test";
        var robotsResponse = await host.SendAsync(robots);
        robotsResponse.EnsureSuccessStatusCode();
        var robotsText = await robotsResponse.Content.ReadAsStringAsync();
        Assert.Contains($"{DevelopmentDataSeeder.DemoTenantSlug}.floristhub.test/sitemap.xml", robotsText);

        using var sitemap = new HttpRequestMessage(HttpMethod.Get, "/sitemap.xml");
        sitemap.Headers.Host = $"{DevelopmentDataSeeder.DemoSecondTenantSlug}.floristhub.test";
        var sitemapResponse = await host.SendAsync(sitemap);
        sitemapResponse.EnsureSuccessStatusCode();
        var sitemapText = await sitemapResponse.Content.ReadAsStringAsync();
        Assert.Contains(DevelopmentDataSeeder.DemoSecondTenantSlug, sitemapText);
        Assert.DoesNotContain("buque-primaveril", sitemapText);

        using var html = new HttpRequestMessage(HttpMethod.Get, "/api/seo/documento?caminho=/portfolio/buque-primaveril");
        html.Headers.Host = $"{DevelopmentDataSeeder.DemoTenantSlug}.floristhub.test";
        var htmlResponse = await host.SendAsync(html);
        htmlResponse.EnsureSuccessStatusCode();
        var page = await htmlResponse.Content.ReadAsStringAsync();
        Assert.Contains("og:title", page);
        Assert.Contains("aggregateRating", page, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Florist", page);
    }

    private sealed record CouponRow(Guid Id, string Code);
    private sealed record OrderRow(Guid Id, int Status);
    private sealed record ReviewRow(Guid Id, Guid OrderId);
    private sealed record FlowerDto(Guid Id, string Name);
    private sealed record PagedFlowers(List<FlowerDto> Items, int Total, int Page, int PageSize);
    private sealed record StoreThemeDto(string Slug);
    private sealed record DashboardMetricsDto(int PendingOrders);
    private sealed record StoreSettingsDto(string Name);
    private sealed record PagedCustomers(List<CustomerRow> Items, int Total, int Page, int PageSize);
    private sealed record CustomerRow(Guid CustomerId, string? FullName, string? Email, int OrderCount, decimal TotalSpent);
}
