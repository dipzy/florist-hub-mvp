using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.Legal;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Email;
using FloristHub.Services.Invoicing;
using FloristHub.Services.Marketing;
using FloristHub.Services.Payments;
using FloristHub.Services.Tests.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Tests.Marketing;

public class MarketingRulesTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb10");

    [Fact]
    public void Percent_and_fixed_discounts_round_half_away_from_zero()
    {
        Assert.Equal(1.00m, CouponMath.Discount(CouponDiscountType.Percent, 10m, 10.00m));
        Assert.Equal(1.01m, CouponMath.Discount(CouponDiscountType.Percent, 10m, 10.05m));
        Assert.Equal(3.33m, CouponMath.Discount(CouponDiscountType.Percent, 33.33m, 9.99m));
        Assert.Equal(0.01m, CouponMath.Discount(CouponDiscountType.Percent, 50m, 0.01m));
        Assert.Equal(4.00m, CouponMath.Discount(CouponDiscountType.FixedAmount, 5m, 4.00m));
        Assert.Equal(0m, CouponMath.Discount(CouponDiscountType.Percent, 10m, 0m));
    }

    [Fact]
    public void Scoped_coupon_only_discounts_matching_lines()
    {
        var lines = new[]
        {
            new CouponLine(Guid.Parse("00000000-0000-0000-0000-000000000001"), "rosas", 20m),
            new CouponLine(Guid.Parse("00000000-0000-0000-0000-000000000002"), "folhagem", 10m)
        };
        var eligible = CouponMath.Eligible(lines, true, new HashSet<Guid>(), new HashSet<string> { "rosas" });
        Assert.Equal(20m, eligible);
        Assert.Equal(2m, CouponMath.Discount(CouponDiscountType.Percent, 10m, eligible));
        Assert.Equal(30m, CouponMath.Eligible(lines, false, new HashSet<Guid>(), new HashSet<string>()));
    }

    [Fact]
    public void Invoice_lines_include_the_coupon_and_sum_to_the_payable_total()
    {
        var order = new Order
        {
            CouponCode = "MAES10",
            DiscountAmount = 3.33m,
            DeliveryFee = 2.50m,
            EstimatedTotal = 32.50m,
            Items =
            {
                new OrderItem { Name = "Rosa", Quantity = 1, UnitPrice = 33.33m, VatRate = 23m }
            }
        };

        var lines = InvoiceService.BuildLines(order, 32.50m, 23m, true);
        var gross = lines.Sum(line => decimal.Round(line.GrossUnitPrice * line.Quantity, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(32.50m, gross);
        Assert.Contains(lines, line => line.Name == "Desconto MAES10" && line.GrossUnitPrice == -3.33m);
        Assert.Contains(lines, line => line.Name == "Entrega");
    }

    [Fact]
    public void Seo_markup_has_open_graph_and_aggregate_rating()
    {
        var json = SeoMarkup.JsonLdFlorist("Flores da Lina", "Florista em Lisboa", "https://flores-lina.floristhub.test/", null, "Rua A", "Lisboa", "1000-001", 4.5m, 2);
        Assert.Contains("\"Florist\"", json);
        Assert.Contains("LocalBusiness", json);
        Assert.Contains("aggregateRating", json);
        Assert.Contains("4.50", json);

        var html = SeoMarkup.Html(new SeoPage("Flores da Lina — florista", "Buquês", "https://flores-lina.floristhub.test/", null, "Flores da Lina", json));
        Assert.Contains("<meta property=\"og:title\" content=\"Flores da Lina — florista\" />", html);
        Assert.Contains("application/ld+json", html);
        Assert.Contains("<link rel=\"canonical\"", html);

        var robots = SeoMarkup.Robots("https://flores-lina.floristhub.test/sitemap.xml");
        Assert.Contains("Sitemap: https://flores-lina.floristhub.test/sitemap.xml", robots);
        var sitemap = SeoMarkup.Sitemap(["https://flores-lina.floristhub.test/portfolio/buque-primaveril"]);
        Assert.Contains("buque-primaveril", sitemap);
        Assert.Equal("buque-primaveril", PublicSlug.From("Buquê primaveril"));
    }

    [Fact]
    public async Task Coupon_changes_stripe_amount_fee_and_blocks_other_tenants()
    {
        var fx = Create();
        var customer = await SeedCustomerAsync(fx, TenantA);
        var rose = new Flower { TenantId = TenantA, Name = "Rosa", UnitPrice = 33.33m, Category = "rosas" };
        fx.Db.Flowers.Add(rose);
        var coupon = new Coupon
        {
            TenantId = TenantA,
            Code = "MAES10",
            DiscountType = CouponDiscountType.Percent,
            DiscountValue = 10m,
            IsActive = true,
            MaxRedemptions = 2,
            MaxRedemptionsPerCustomer = 1,
            MinimumOrderAmount = 20m
        };
        fx.Db.Coupons.Add(coupon);
        fx.Db.Coupons.Add(new Coupon
        {
            TenantId = TenantB,
            Code = "OUTRA",
            DiscountType = CouponDiscountType.FixedAmount,
            DiscountValue = 5m,
            IsActive = true
        });
        var order = NewOrder(TenantA, customer.Id, rose.Id, 33.33m, 2.50m);
        fx.Db.Orders.Add(order);
        await fx.Db.SaveChangesAsync();

        await OrderPricing.ApplyCouponAsync(fx.Uow, order, "maes10", customer.Id, CancellationToken.None);
        await fx.Db.SaveChangesAsync();
        Assert.Equal(3.33m, order.DiscountAmount);
        Assert.Equal(32.50m, order.EstimatedTotal);

        var session = await fx.Payments.StartCheckoutAsync(order.Id, customer.Id, "https://loja.test/ok", "https://loja.test/cancel");
        Assert.True(session.PaymentsEnabled);
        Assert.Equal(3250, fx.Stripe.Checkouts.Single().AmountCents);
        Assert.Equal(325, fx.Stripe.Checkouts.Single().ApplicationFeeCents);
        Assert.Equal(32.50m, (await fx.Db.Payments.IgnoreQueryFilters().SingleAsync()).Amount);

        var second = NewOrder(TenantA, customer.Id, rose.Id, 30m, 0m);
        fx.Db.Orders.Add(second);
        await fx.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OrderPricing.ApplyCouponAsync(fx.Uow, second, "MAES10", customer.Id, CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OrderPricing.ApplyCouponAsync(fx.Uow, second, "OUTRA", customer.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Applying_a_coupon_cancels_a_pending_checkout()
    {
        var fx = Create();
        var customer = await SeedCustomerAsync(fx, TenantA);
        var order = NewOrder(TenantA, customer.Id, null, 40m, 0m);
        fx.Db.Orders.Add(order);
        fx.Db.Coupons.Add(new Coupon
        {
            TenantId = TenantA,
            Code = "MENOS5",
            DiscountType = CouponDiscountType.FixedAmount,
            DiscountValue = 5m,
            IsActive = true
        });
        fx.Db.Payments.Add(new Payment
        {
            TenantId = TenantA,
            OrderId = order.Id,
            Status = PaymentStatus.Pending,
            Amount = 40m,
            Currency = "eur",
            CheckoutUrl = "https://checkout.stripe.test/old",
            StripeCheckoutSessionId = "cs_old"
        });
        await fx.Db.SaveChangesAsync();

        await OrderPricing.ApplyCouponAsync(fx.Uow, order, "MENOS5", customer.Id, CancellationToken.None);
        await fx.Db.SaveChangesAsync();

        Assert.Equal(35m, order.EstimatedTotal);
        Assert.Equal(PaymentStatus.Cancelled, (await fx.Db.Payments.IgnoreQueryFilters().SingleAsync(p => p.StripeCheckoutSessionId == "cs_old")).Status);
    }

    [Fact]
    public async Task Category_coupon_and_campaign_price_are_scoped_to_the_order()
    {
        var fx = Create();
        var customer = await SeedCustomerAsync(fx, TenantA);
        var rose = new Flower { TenantId = TenantA, Name = "Rosa", UnitPrice = 10m, Category = "rosas", SeasonMonths = SeasonMonths.ToMask([2, 5]) };
        var leaf = new Flower { TenantId = TenantA, Name = "Folha", UnitPrice = 4m, Category = "folhas" };
        fx.Db.Flowers.AddRange(rose, leaf);
        var campaign = new SeasonalCampaign
        {
            TenantId = TenantA,
            Name = "São Valentim",
            Slug = "sao-valentim",
            StartsOn = LisbonClock.Today().AddDays(-1),
            EndsOn = LisbonClock.Today().AddDays(1),
            HighlightOnHome = true,
            IsActive = true
        };
        campaign.Flowers.Add(new CampaignFlower { TenantId = TenantA, FlowerId = rose.Id, SpecialPrice = 8m });
        fx.Db.SeasonalCampaigns.Add(campaign);
        fx.Db.Coupons.Add(new Coupon
        {
            TenantId = TenantA,
            Code = "ROSAS",
            DiscountType = CouponDiscountType.Percent,
            DiscountValue = 25m,
            IsActive = true,
            Targets = { new CouponTarget { TenantId = TenantA, Category = "rosas" } }
        });
        var order = NewOrder(TenantA, customer.Id, rose.Id, 10m, 0m);
        order.Items.Add(new OrderItem { TenantId = TenantA, FlowerId = leaf.Id, Name = "Folha", Quantity = 1, UnitPrice = 4m });
        fx.Db.Orders.Add(order);
        await fx.Db.SaveChangesAsync();

        await OrderPricing.ApplySpecialPricesAsync(fx.Uow, order, CancellationToken.None);
        await OrderPricing.ApplyCouponAsync(fx.Uow, order, "ROSAS", customer.Id, CancellationToken.None);
        Assert.Equal(8m, order.Items.Single(i => i.FlowerId == rose.Id).UnitPrice);
        Assert.Equal(2m, order.DiscountAmount);
        Assert.Equal(10m, order.EstimatedTotal);
        Assert.True(SeasonMonths.Includes(rose.SeasonMonths, 2));
        Assert.False(SeasonMonths.Includes(rose.SeasonMonths, 3));
    }

    [Fact]
    public async Task Exhausted_flowers_cannot_be_bought_with_a_campaign_price_or_a_coupon()
    {
        var fx = Create();
        var customer = await SeedCustomerAsync(fx, TenantA);
        var rose = new Flower { TenantId = TenantA, Name = "Rosa", UnitPrice = 10m, StockQuantity = 0, Category = "rosas" };
        fx.Db.Flowers.Add(rose);
        var campaign = new SeasonalCampaign
        {
            TenantId = TenantA,
            Name = "São Valentim",
            Slug = "sao-valentim-esgotada",
            StartsOn = LisbonClock.Today().AddDays(-1),
            EndsOn = LisbonClock.Today().AddDays(1),
            IsActive = true
        };
        campaign.Flowers.Add(new CampaignFlower { TenantId = TenantA, FlowerId = rose.Id, SpecialPrice = 8m });
        fx.Db.SeasonalCampaigns.Add(campaign);
        fx.Db.Coupons.Add(new Coupon
        {
            TenantId = TenantA,
            Code = "ROSAS",
            DiscountType = CouponDiscountType.Percent,
            DiscountValue = 10m,
            IsActive = true
        });
        var order = NewOrder(TenantA, customer.Id, rose.Id, 10m, 0m);
        fx.Db.Orders.Add(order);
        await fx.Db.SaveChangesAsync();

        var campaignError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OrderPricing.ApplySpecialPricesAsync(fx.Uow, order, CancellationToken.None));
        Assert.Contains("esgotada", campaignError.Message);
        Assert.Equal(10m, order.Items.Single().UnitPrice);

        var couponError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OrderPricing.ApplyCouponAsync(fx.Uow, order, "ROSAS", customer.Id, CancellationToken.None));
        Assert.Contains("cupão", couponError.Message);
        Assert.Null(order.CouponId);
    }

    [Fact]
    public async Task Only_the_customer_of_a_delivered_order_can_review_it()
    {
        var fx = Create();
        var customer = await SeedCustomerAsync(fx, TenantA);
        var other = await SeedCustomerAsync(fx, TenantA);
        var delivered = NewOrder(TenantA, customer.Id, null, 10m, 0m);
        delivered.Status = OrderStatus.Delivered;
        var pending = NewOrder(TenantA, customer.Id, null, 10m, 0m);
        fx.Db.Orders.AddRange(delivered, pending);
        await fx.Db.SaveChangesAsync();
        var reviews = new ReviewService(fx.Uow, fx.Tenant, new AuditLogger(fx.Uow, NullLogger<AuditLogger>.Instance));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reviews.CreateAsync(pending.Id, customer.Id, new CreateReviewRequest(5, "cedo"), CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            reviews.CreateAsync(delivered.Id, other.Id, new CreateReviewRequest(5, "não"), CancellationToken.None));

        var created = await reviews.CreateAsync(delivered.Id, customer.Id, new CreateReviewRequest(4, "Lindo"), CancellationToken.None);
        await reviews.ApproveAsync(created.Id, Guid.NewGuid(), CancellationToken.None);
        await reviews.ReplyAsync(created.Id, "Obrigado", Guid.NewGuid(), CancellationToken.None);
        await reviews.SetHiddenAsync(created.Id, true, "abuso", Guid.NewGuid(), CancellationToken.None);
        var summary = await reviews.SummaryAsync(CancellationToken.None);
        Assert.Equal(0, summary.ReviewCount);
        Assert.Empty(await reviews.ListPublicAsync(CancellationToken.None));

        fx.Tenant.SetTenant(TenantB, "loja-b");
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            reviews.ReplyAsync(created.Id, "invasão", Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Sitemap_lists_only_the_current_tenant()
    {
        var fx = Create();
        fx.Db.PortfolioArrangements.AddRange(
            new PortfolioArrangement { TenantId = TenantA, Title = "Buquê", Slug = "buque-primaveril", Status = PortfolioArrangementStatus.Active },
            new PortfolioArrangement { TenantId = TenantB, Title = "Outro", Slug = "outro", Status = PortfolioArrangementStatus.Active });
        fx.Db.SeasonalCampaigns.Add(new SeasonalCampaign
        {
            TenantId = TenantA,
            Name = "Dia da Mãe",
            Slug = "dia-da-mae",
            StartsOn = LisbonClock.Today(),
            EndsOn = LisbonClock.Today(),
            IsActive = true,
            HighlightOnHome = true
        });
        await fx.Db.SaveChangesAsync();
        var seo = new SeoService(fx.Uow, fx.Tenant, new ReviewService(fx.Uow, fx.Tenant, new AuditLogger(fx.Uow, NullLogger<AuditLogger>.Instance)));
        var sitemap = await seo.SitemapAsync("https://loja-a.floristhub.test", CancellationToken.None);
        Assert.Contains("buque-primaveril", sitemap);
        Assert.Contains("dia-da-mae", sitemap);
        Assert.DoesNotContain("outro", sitemap);
        var html = await seo.DocumentAsync("https://loja-a.floristhub.test", "/portfolio/buque-primaveril", "https://api.test", CancellationToken.None);
        Assert.Contains("og:title", html);
        Assert.Contains("Loja A", html);
        Assert.Contains("Florist", html);
    }

    private static Order NewOrder(Guid tenantId, Guid customerId, Guid? flowerId, decimal unitPrice, decimal delivery)
    {
        var order = new Order
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Status = OrderStatus.Confirmed,
            DeliveryType = DeliveryType.HomeDelivery,
            BaseType = ArrangementBaseType.Sponge,
            DeliveryFee = delivery,
            EstimatedTotal = unitPrice + delivery
        };
        order.Items.Add(new OrderItem
        {
            TenantId = tenantId,
            FlowerId = flowerId,
            Name = "Rosa",
            Quantity = 1,
            UnitPrice = unitPrice,
            VatRate = 23m
        });
        return order;
    }

    private static async Task<ApplicationUser> SeedCustomerAsync(Fixture fx, Guid tenantId)
    {
        var id = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = id,
            Email = $"c-{id:N}@loja.pt",
            UserName = $"c-{id:N}@loja.pt",
            FullName = "Ana Costa",
            TenantId = tenantId
        };
        fx.Db.Users.Add(user);
        await fx.Db.SaveChangesAsync();
        return user;
    }

    private static Fixture Create()
    {
        var tenant = new TenantContext();
        tenant.SetTenant(TenantA, "loja-a");
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        db.Tenants.AddRange(
            new Tenant { Id = TenantA, Name = "Loja A", Slug = "loja-a", ContactEmail = "a@loja.pt", StripeAccountId = "acct_a", StripeChargesEnabled = true, City = "Lisboa", IsPublished = true },
            new Tenant { Id = TenantB, Name = "Loja B", Slug = "loja-b", StripeAccountId = "acct_b", StripeChargesEnabled = true, IsPublished = true });
        db.LegalDocumentVersions.AddRange(
            new LegalDocumentVersion { DocumentType = LegalDocumentType.Terms, Version = LegalDocumentSeedContent.SeedVersion, Title = "Termos", ContentMarkdown = "t", IsCurrent = true },
            new LegalDocumentVersion { DocumentType = LegalDocumentType.Privacy, Version = LegalDocumentSeedContent.SeedVersion, Title = "Privacidade", ContentMarkdown = "p", IsCurrent = true });
        db.SaveChanges();
        var uow = new UnitOfWork(db);
        var stripe = new FakeStripeGateway { IsConfigured = true };
        var payments = new PaymentService(
            uow,
            tenant,
            stripe,
            new OrderEmailNotificationsService(new InMemoryEmailQueue(), Options.Create(new EmailOptions())),
            new NoOrderPushNotifications(),
            new AuditLogger(uow, NullLogger<AuditLogger>.Instance),
            new Notifications.NoFloristPanelNotificationService(),
            Options.Create(new EmailOptions { WebBaseUrl = "https://loja.test" }),
            Options.Create(new StripeOptions { PublishableKey = "pk_test_example" }),
            new RecordingInvoices());
        return new Fixture(db, uow, tenant, stripe, payments);
    }

    private sealed record Fixture(ApplicationDbContext Db, UnitOfWork Uow, TenantContext Tenant, FakeStripeGateway Stripe, PaymentService Payments);

    private sealed class RecordingInvoices : IInvoiceIssuance
    {
        public Task EnsureInvoiceReceiptAsync(Guid paymentId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EnsureCreditNoteAsync(Guid paymentId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
