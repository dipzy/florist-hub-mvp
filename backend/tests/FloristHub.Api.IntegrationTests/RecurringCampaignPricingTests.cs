using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.Persistence;
using FloristHub.Services.Marketing;
using FloristHub.Services.Recurring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class RecurringCampaignPricingTests
{
    private readonly FloristHubPostgresFixture _fixture;

    public RecurringCampaignPricingTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Generated_recurring_order_takes_the_campaign_price_and_no_coupon()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = ApiTestClient.Demo.TenantLina;
        var delivery = NextOpenNoon();

        var campaign = new SeasonalCampaign
        {
            TenantId = tenantId,
            Name = "Dia da Mãe",
            Slug = "dia-da-mae-" + Guid.NewGuid().ToString("N")[..8],
            StartsOn = LisbonClock.Today().AddDays(-1),
            EndsOn = LisbonClock.Today().AddDays(14),
            IsActive = true,
            HighlightOnHome = true
        };
        campaign.Flowers.Add(new CampaignFlower
        {
            TenantId = tenantId,
            FlowerId = ApiTestClient.Demo.RoseLina,
            SpecialPrice = 1.25m
        });
        db.SeasonalCampaigns.Add(campaign);
        db.Coupons.Add(new Coupon
        {
            TenantId = tenantId,
            Code = "S" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            DiscountType = CouponDiscountType.Percent,
            DiscountValue = 50m,
            IsActive = true
        });

        var subscription = new RecurringSubscription
        {
            TenantId = tenantId,
            CustomerId = ApiTestClient.Demo.CustomerUser,
            Status = RecurringSubscriptionStatus.Active,
            Frequency = RecurringFrequency.Weekly,
            AnchorDeliveryAt = delivery,
            NextDeliveryAt = delivery,
            DeliveryType = DeliveryType.StorePickup,
            BaseType = ArrangementBaseType.Sponge
        };
        subscription.Items.Add(new RecurringSubscriptionItem
        {
            TenantId = tenantId,
            FlowerId = ApiTestClient.Demo.RoseLina,
            Name = "Rosa vermelha",
            Quantity = 2,
            UnitPrice = 2.50m
        });
        db.RecurringSubscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var generated = await scope.ServiceProvider
            .GetRequiredService<IRecurringOrderGenerationService>()
            .ProcessDueAsync();
        Assert.True(generated >= 1);

        var order = await db.Orders.IgnoreQueryFilters()
            .Include(o => o.Items)
            .SingleAsync(o => o.RecurringSubscriptionId == subscription.Id);
        Assert.Equal(1.25m, Assert.Single(order.Items).UnitPrice);
        Assert.Equal(2.50m, order.EstimatedTotal);
        Assert.Equal(0m, order.DiscountAmount);
        Assert.Null(order.CouponId);
        Assert.Null(order.CouponCode);
    }

    private static DateTimeOffset NextOpenNoon()
    {
        var delivery = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1).AddHours(12), TimeSpan.Zero);
        if (delivery.DayOfWeek == DayOfWeek.Sunday)
            delivery = delivery.AddDays(1);
        return delivery;
    }
}
