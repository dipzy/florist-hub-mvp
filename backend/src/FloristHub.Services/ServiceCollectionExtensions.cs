using FloristHub.Services.Auth;
using FloristHub.Services.Capacity;
using FloristHub.Services.Email;
using FloristHub.Services.Stock;
using FloristHub.Services.Push;
using FloristHub.Services.Recurring;
using FloristHub.Services.Interfaces.Payments;
using FloristHub.Services.Interfaces.Visualization;
using FloristHub.Services.Invoicing;
using FloristHub.Services.Marketing;
using FloristHub.Services.Notifications;
using FloristHub.Services.Onboarding;
using FloristHub.Services.Orders;
using FloristHub.Services.Payments;
using FloristHub.Services.Storage;
using FloristHub.Services.Visualization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FloristHub.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFloristHubServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFloristMembershipService, FloristMembershipService>();
        services.AddSingleton<CapacityGate>();
        services.AddSingleton<StockGate>();
        services.AddScoped<IRelationalWork, RelationalWork>();
        services.AddScoped<IDeliveryCapacityService, DeliveryCapacityService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IFlowerService, FlowerService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderTrackingService, OrderTrackingService>();
        services.AddScoped<IFloristPanelNotificationService, FloristPanelNotificationService>();
        services.AddScoped<IFloristStoreSettingsService, FloristStoreSettingsService>();
        services.AddScoped<IStoreCheckoutInfoService, StoreCheckoutInfoService>();
        services.AddScoped<IPublicStoreThemeService, PublicStoreThemeService>();
        services.AddScoped<IPlatformTenantService, PlatformTenantService>();
        services.AddScoped<IFloristInviteService, FloristInviteService>();
        services.AddScoped<IFloristInviteStore, FloristInviteStore>();
        services.AddScoped<IFloristOnboardingService, FloristOnboardingService>();
        services.TryAddSingleton<Interfaces.ICorsDomainCacheInvalidator, Interfaces.NullCorsDomainCacheInvalidator>();
        services.AddScoped<IFloristDashboardService, FloristDashboardService>();
        services.AddScoped<IFloristCustomersService, FloristCustomersService>();
        services.AddScoped<ILegalComplianceService, LegalComplianceService>();
        services.AddScoped<IDataSubjectRightsService, DataSubjectRightsService>();
        services.AddScoped<IComplianceAuditService, ComplianceAuditService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IPublicStorefrontService, PublicStorefrontService>();
        services.AddScoped<ISeoService, SeoService>();
        services.AddScoped<IRecurringSubscriptionService, RecurringSubscriptionService>();
        services.AddScoped<IPlatformMetricsService, PlatformMetricsService>();
        services.Configure<RecurringOrderOptions>(configuration.GetSection(RecurringOrderOptions.SectionName));
        services.AddScoped<IRecurringOrderGenerationService, RecurringOrderGenerationService>();
        services.AddHostedService<RecurringOrderGenerationHostedService>();

        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services.AddOptions<InvoicingOptions>()
            .Bind(configuration.GetSection(InvoicingOptions.SectionName))
            .PostConfigure(options =>
            {
                if (options.AllowFakeProvider is not null)
                    return;
                var environment = configuration["ASPNETCORE_ENVIRONMENT"]
                    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                options.AllowFakeProvider = !string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
            });
        services.AddHttpClient<InvoiceXpressInvoicingProvider>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .RemoveAllLoggers();
        services.AddSingleton<FakeInvoicingProvider>();
        services.AddScoped<IInvoicingProviderFactory, InvoicingProviderFactory>();
        services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoiceIssuance>(sp => sp.GetRequiredService<IInvoiceService>());
        services.AddHostedService<InvoiceRetryWorker>();
        var stripeProvider = configuration["Stripe:Provider"];
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var allowFakeStripe = string.Equals(stripeProvider, "Fake", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
        if (allowFakeStripe)
            services.AddSingleton<IStripeGateway, DevelopmentStripeGateway>();
        else if (string.IsNullOrWhiteSpace(configuration["Stripe:SecretKey"]))
            services.AddSingleton<IStripeGateway, DisabledStripeGateway>();
        else
            services.AddSingleton<IStripeGateway, StripeGateway>();

        services.AddSingleton<IPaymentGateway, NoOpPaymentGateway>();
        services.AddSingleton<IArrangement3DService, NoOpArrangement3DService>();
        services.AddFloristHubFileStorage(configuration);
        services.AddFloristHubEmail(configuration);
        services.AddFloristHubPush(configuration);

        return services;
    }
}
