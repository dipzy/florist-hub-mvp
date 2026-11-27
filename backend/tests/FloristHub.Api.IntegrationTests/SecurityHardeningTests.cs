using System.Net;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.Legal;
using FloristHub.DAL.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class SecurityHardeningTests
{
    private readonly FloristHubPostgresFixture _fixture;

    public SecurityHardeningTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Public_catalog_without_tenant_does_not_leak_other_stores()
    {
        var marker = "segredo-" + Guid.NewGuid().ToString("N")[..8];
        var florist = Florist();
        var created = await florist.SendJsonAsync(HttpMethod.Post, "/api/florist/campaigns", new
        {
            name = marker,
            description = "Só desta loja",
            startsOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            endsOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            highlightOnHome = true,
            isActive = true,
            flowers = new[] { new { flowerId = ApiTestClient.Demo.RoseLina, specialPrice = 1.1m, sortOrder = 0 } }
        });
        created.EnsureSuccessStatusCode();
        var campaign = await florist.ReadJsonAsync<CampaignBody>(created);

        var anonymous = new ApiTestClient(_fixture.Factory.CreateClient());
        var reviews = await anonymous.GetAsync("/api/store/reviews");
        var seasonal = await anonymous.GetAsync("/api/store/seasonal");
        var home = await anonymous.GetAsync("/api/store/home");
        var flowers = await anonymous.GetAsync("/api/flowers");
        var bySlug = await anonymous.GetAsync("/api/store/campaigns/" + campaign!.Slug);
        Assert.Equal(HttpStatusCode.BadRequest, reviews.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, seasonal.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, home.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, flowers.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, bySlug.StatusCode);

        var leaked = await reviews.Content.ReadAsStringAsync();
        Assert.DoesNotContain(marker, leaked, StringComparison.Ordinal);
        var removed = await florist.SendJsonAsync(HttpMethod.Delete, $"/api/florist/campaigns/{campaign.Id}");
        removed.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Parallel_coupon_uses_cannot_pass_the_maximum()
    {
        var florist = Florist();
        await florist.SendJsonAsync(HttpMethod.Put, "/api/florist/capacity/settings", new
        {
            dailyOrderLimit = 400,
            windows = Array.Empty<object>()
        });
        var code = "R" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var coupon = await florist.SendJsonAsync(HttpMethod.Post, "/api/florist/coupons", new
        {
            code,
            discountType = 0,
            discountValue = 10m,
            maxRedemptions = 1,
            isActive = true,
            targets = Array.Empty<object>()
        });
        coupon.EnsureSuccessStatusCode();

        var negative = await florist.SendJsonAsync(HttpMethod.Post, "/api/florist/coupons", new
        {
            code = "N" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            discountType = 1,
            discountValue = -5m,
            isActive = true,
            targets = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);

        var first = Customer();
        var second = Customer();
        var orderA = await PostOrder(first, DeliveryAt(3));
        var orderB = await PostOrder(second, DeliveryAt(4));
        orderA.EnsureSuccessStatusCode();
        orderB.EnsureSuccessStatusCode();
        var idA = (await first.ReadJsonAsync<IdBody>(orderA))!.Id;
        var idB = (await second.ReadJsonAsync<IdBody>(orderB))!.Id;

        var applied = await Task.WhenAll(
            first.SendJsonAsync(HttpMethod.Post, $"/api/orders/{idA}/coupon", new { code }),
            second.SendJsonAsync(HttpMethod.Post, $"/api/orders/{idB}/coupon", new { code }));
        var bodies = await Task.WhenAll(applied.Select(async response => $"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}"));
        Assert.True(applied.Count(r => r.StatusCode == HttpStatusCode.OK) == 1, string.Join(" | ", bodies));
        Assert.Contains(applied, r => r.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Parallel_confirms_on_two_processes_cannot_oversell_stock()
    {
        var florist = Florist();
        await florist.SendJsonAsync(HttpMethod.Put, "/api/florist/capacity/settings", new
        {
            dailyOrderLimit = 400,
            windows = Array.Empty<object>()
        });
        var created = await florist.SendJsonAsync(HttpMethod.Post, "/api/flowers", new
        {
            name = "Única " + Guid.NewGuid().ToString("N")[..6],
            color = "Branco",
            unitPrice = 3m,
            unit = "haste",
            stockQuantity = 1,
            isApproximatelyAvailable = true
        });
        created.EnsureSuccessStatusCode();
        var flowerId = (await florist.ReadJsonAsync<IdBody>(created))!.Id;

        await using var otherFactory = _fixture.CreateFactory();
        using var otherClient = otherFactory.CreateClient();
        var floristB = new ApiTestClient(otherClient);
        floristB.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristLinaUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.FloristLinaEmail);

        var buyerA = Customer();
        var buyerB = Customer();
        var orderA = await PostOrder(buyerA, DeliveryAt(5), flowerId);
        var orderB = await PostOrder(buyerB, DeliveryAt(6), flowerId);
        orderA.EnsureSuccessStatusCode();
        orderB.EnsureSuccessStatusCode();
        var idA = (await buyerA.ReadJsonAsync<IdBody>(orderA))!.Id;
        var idB = (await buyerB.ReadJsonAsync<IdBody>(orderB))!.Id;

        var confirmed = await Task.WhenAll(
            florist.SendJsonAsync(HttpMethod.Post, $"/api/orders/{idA}/confirm", new { notes = "a" }),
            floristB.SendJsonAsync(HttpMethod.Post, $"/api/orders/{idB}/confirm", new { notes = "b" }));
        Assert.Equal(1, confirmed.Count(r => r.StatusCode == HttpStatusCode.OK));

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stock = await db.Flowers.IgnoreQueryFilters().Where(f => f.Id == flowerId).Select(f => f.StockQuantity).SingleAsync();
        Assert.Equal(0, stock);
    }

    [Fact]
    public async Task Recurring_subscription_is_not_readable_across_customers_or_by_superadmin()
    {
        var customer = Customer();
        var when = DeliveryAt(7);
        var created = await customer.SendJsonAsync(HttpMethod.Post, "/api/recurring-subscriptions", new
        {
            frequency = 0,
            anchorDeliveryAt = when,
            deliveryType = 0,
            baseType = 0,
            isFloristChoice = true,
            floristChoiceBudget = 20m,
            items = Array.Empty<object>(),
            acceptTermsAndPrivacy = true,
            termsVersion = LegalDocumentSeedContent.SeedVersion,
            privacyVersion = LegalDocumentSeedContent.SeedVersion
        });
        created.EnsureSuccessStatusCode();
        var id = (await customer.ReadJsonAsync<IdBody>(created))!.Id;

        var other = Customer();
        other.UseTestPrincipal(Roles.Customer, Guid.NewGuid(), ApiTestClient.Demo.TenantLina, "outro@demo.floristhub.pt");
        var foreign = await other.GetAsync($"/api/recurring-subscriptions/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);

        var admin = new ApiTestClient(_fixture.Factory.CreateClient());
        admin.UseTestPrincipal(Roles.SuperAdmin, Guid.NewGuid(), Guid.Empty, "admin@demo.floristhub.pt");
        var elevated = await admin.GetAsync($"/api/recurring-subscriptions/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, elevated.StatusCode);

        var campo = new ApiTestClient(_fixture.Factory.CreateClient());
        campo.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristCampoUser, ApiTestClient.Demo.TenantCampo, ApiTestClient.Demo.FloristCampoEmail);
        var otherStore = await campo.GetAsync($"/api/recurring-subscriptions/{id}");
        Assert.True(otherStore.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymize_clears_tax_id_and_review_comment()
    {
        var userId = Guid.NewGuid();
        var email = $"anon-{userId:N}@demo.floristhub.pt";
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Ana Teste",
                TenantId = ApiTestClient.Demo.TenantLina
            };
            var result = await users.CreateAsync(user, ApiTestClient.Demo.Password);
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
            Assert.True((await users.AddToRoleAsync(user, Roles.Customer)).Succeeded);

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = new Order
            {
                TenantId = ApiTestClient.Demo.TenantLina,
                CustomerId = userId,
                Status = OrderStatus.Delivered,
                BaseType = ArrangementBaseType.Sponge,
                DeliveryType = DeliveryType.StorePickup,
                EstimatedTotal = 12m,
                CustomerTaxId = "999999990"
            };
            db.Orders.Add(order);
            db.OrderReviews.Add(new OrderReview
            {
                TenantId = ApiTestClient.Demo.TenantLina,
                OrderId = order.Id,
                CustomerId = userId,
                Stars = 4,
                Comment = "Comentário com dados pessoais"
            });
            await db.SaveChangesAsync();
        }

        var client = Customer();
        client.UseTestPrincipal(Roles.Customer, userId, ApiTestClient.Demo.TenantLina, email);
        var anonymized = await client.SendJsonAsync(HttpMethod.Post, "/api/privacy/anonymize");
        anonymized.EnsureSuccessStatusCode();

        using var check = _fixture.Factory.Services.CreateScope();
        var after = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await after.Orders.IgnoreQueryFilters().SingleAsync(o => o.CustomerId == userId);
        var review = await after.OrderReviews.IgnoreQueryFilters().SingleAsync(r => r.CustomerId == userId);
        Assert.Null(stored.CustomerTaxId);
        Assert.Null(stored.DeliveryAddress);
        Assert.Null(review.Comment);
    }

    private ApiTestClient Florist()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Florist, ApiTestClient.Demo.FloristLinaUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.FloristLinaEmail);
        return client;
    }

    private ApiTestClient Customer()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Customer, ApiTestClient.Demo.CustomerUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.CustomerEmail);
        return client;
    }

    private static Task<HttpResponseMessage> PostOrder(ApiTestClient client, DateTimeOffset when, Guid? flowerId = null) =>
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
                new { flowerId, name = "Rosa vermelha", quantity = 1, unitPrice = 2.5m }
            }
        });

    private static DateTimeOffset DeliveryAt(int lane)
    {
        var day = DateTime.UtcNow.Date.AddDays(40 + lane);
        while (day.DayOfWeek == DayOfWeek.Sunday)
            day = day.AddDays(1);
        return new DateTimeOffset(day.AddHours(11), TimeSpan.Zero);
    }

    private sealed record IdBody(Guid Id);
    private sealed record CampaignBody(Guid Id, string Slug);
}
