using System.Net;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Legal;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class CapacityAndStockTests
{
    private readonly FloristHubPostgresFixture _fixture;

    public CapacityAndStockTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Checkout_rejects_a_full_day_and_parallel_creates_take_only_one_slot()
    {
        var florist = FloristLina();
        var when = DeliveryAt(0);
        var from = DateOnly.FromDateTime(when.Date);
        await florist.SendJsonAsync(HttpMethod.Put, "/api/florist/capacity/settings", new
        {
            dailyOrderLimit = 400,
            windows = Array.Empty<object>()
        });
        var before = await florist.ReadJsonAsync<List<CapacityDay>>(
            await florist.GetAsync($"/api/florist/capacity?from={from:yyyy-MM-dd}&to={from:yyyy-MM-dd}"));
        await florist.SendJsonAsync(HttpMethod.Put, "/api/florist/capacity/settings", new
        {
            dailyOrderLimit = before![0].Booked + 1,
            windows = Array.Empty<object>()
        });

        var first = Customer();
        var second = Customer();
        var created = await Task.WhenAll(PostOrder(first, when), PostOrder(second, when));
        var bodies = await Task.WhenAll(created.Select(async response => $"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}"));
        Assert.True(created.Count(r => r.StatusCode == HttpStatusCode.OK) == 1, string.Join(" | ", bodies));
        Assert.Contains(created, r => r.StatusCode == HttpStatusCode.BadRequest);

        var calendar = await florist.ReadJsonAsync<List<CapacityDay>>(
            await florist.GetAsync($"/api/florist/capacity?from={from:yyyy-MM-dd}&to={from:yyyy-MM-dd}"));
        Assert.Equal(0, calendar![0].Remaining);

        var campo = FloristCampo();
        var other = await campo.ReadJsonAsync<CapacitySettings>(await campo.GetAsync("/api/florist/capacity/settings"));
        Assert.Null(other!.DailyOrderLimit);
        var leak = await campo.GetAsync($"/api/florist/capacity?from={from:yyyy-MM-dd}&to={from:yyyy-MM-dd}");
        leak.EnsureSuccessStatusCode();
        var campoDays = await campo.ReadJsonAsync<List<CapacityDay>>(leak);
        Assert.Null(campoDays![0].DailyLimit);

        await florist.SendJsonAsync(HttpMethod.Put, "/api/florist/capacity/settings", new
        {
            dailyOrderLimit = 400,
            windows = Array.Empty<object>()
        });
    }

    [Fact]
    public async Task Confirm_consumes_stock_and_cancel_puts_it_back_without_crossing_tenants()
    {
        var florist = FloristLina();
        var rose = ApiTestClient.Demo.RoseLina;
        var updated = await florist.SendJsonAsync(HttpMethod.Put, $"/api/flowers/{rose}", new
        {
            name = "Rosa vermelha",
            color = "Vermelho",
            unitPrice = 2.5m,
            unit = "ramo",
            stockQuantity = 3,
            minimumStockQuantity = 2,
            isApproximatelyAvailable = true,
            isCatalogActive = true
        });
        updated.EnsureSuccessStatusCode();

        var when = DeliveryAt(1);
        var customer = Customer();
        var created = await PostOrder(customer, when, rose, 2);
        created.EnsureSuccessStatusCode();
        var order = await customer.ReadJsonAsync<OrderId>(created);

        var confirmed = await florist.SendJsonAsync(HttpMethod.Post, $"/api/orders/{order!.Id}/confirm", new { notes = "ok" });
        confirmed.EnsureSuccessStatusCode();
        var after = await florist.ReadJsonAsync<List<Movement>>(await florist.GetAsync($"/api/flowers/{rose}/movements"));
        Assert.Contains(after!, m => m.Delta == -2);

        var alerts = await florist.ReadJsonAsync<List<Alert>>(await florist.GetAsync("/api/flowers/alerts"));
        Assert.Contains(alerts!, a => a.FlowerId == rose);

        var campo = FloristCampo();
        var hidden = await campo.GetAsync($"/api/flowers/{rose}/movements");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        customer.UseTestPrincipal(Roles.Customer, ApiTestClient.Demo.CustomerUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.CustomerEmail);
        var cancelled = await customer.SendJsonAsync(HttpMethod.Post, $"/api/orders/{order.Id}/cancel", new { notes = "Desisti" });
        cancelled.EnsureSuccessStatusCode();
        var restored = await florist.ReadJsonAsync<List<Movement>>(await florist.GetAsync($"/api/flowers/{rose}/movements"));
        Assert.Contains(restored!, m => m.Delta == 2);

        await florist.SendJsonAsync(HttpMethod.Put, $"/api/flowers/{rose}", new
        {
            name = "Rosa vermelha",
            color = "Vermelho",
            unitPrice = 2.5m,
            unit = "ramo",
            stockQuantity = 120,
            isApproximatelyAvailable = true,
            isCatalogActive = true
        });
    }

    private ApiTestClient FloristLina()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristLinaUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.FloristLinaEmail);
        return client;
    }

    private ApiTestClient FloristCampo()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristCampoUser, ApiTestClient.Demo.TenantCampo, ApiTestClient.Demo.FloristCampoEmail);
        return client;
    }

    private ApiTestClient Customer()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Customer, ApiTestClient.Demo.CustomerUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.CustomerEmail);
        return client;
    }

    private static Task<HttpResponseMessage> PostOrder(ApiTestClient client, DateTimeOffset when, Guid? flowerId = null, int quantity = 1) =>
        client.SendJsonAsync(HttpMethod.Post, "/api/orders", new
        {
            baseType = 0,
            deliveryType = 0,
            requestedDeliveryAt = when,
            acceptTermsAndPrivacy = true,
            termsVersion = LegalDocumentSeedContent.SeedVersion,
            privacyVersion = LegalDocumentSeedContent.SeedVersion,
            items = new[]
            {
                new { flowerId, name = "Rosa vermelha", quantity, unitPrice = 2.5m }
            }
        });

    private static DateTimeOffset DeliveryAt(int lane)
    {
        var shift = 20 + lane * 35 + (int)(DateTime.UtcNow.Ticks % 25);
        var day = DateTime.UtcNow.Date.AddDays(shift);
        while (day.DayOfWeek == DayOfWeek.Sunday)
            day = day.AddDays(1);
        return new DateTimeOffset(day.AddHours(11), TimeSpan.Zero);
    }

    private sealed record CapacityDay(DateOnly Date, bool Closed, int? DailyLimit, int Booked, int? Remaining);
    private sealed record CapacitySettings(int? DailyOrderLimit);
    private sealed record OrderId(Guid Id);
    private sealed record Movement(int Delta);
    private sealed record Alert(Guid FlowerId, string Name, int StockQuantity, int MinimumStockQuantity);
}
