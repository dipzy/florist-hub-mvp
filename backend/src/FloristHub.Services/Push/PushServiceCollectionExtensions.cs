using FloristHub.Services.Interfaces.Push;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FloristHub.Services.Push;

public static class PushServiceCollectionExtensions
{
    public static IServiceCollection AddFloristHubPush(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PushOptions>(configuration.GetSection(PushOptions.SectionName));
        services.AddHttpClient(FcmPushSender.HttpClientName);

        services.AddSingleton<IPushSender>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PushOptions>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("FloristHub.Push");
            if (options.IsFcmReady())
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var http = factory.CreateClient(FcmPushSender.HttpClientName);
                return ActivatorUtilities.CreateInstance<FcmPushSender>(sp, http);
            }

            if (options.Provider.Equals("Fcm", StringComparison.OrdinalIgnoreCase))
                logger.LogWarning("Push:Provider=Fcm sem credenciais completas. A usar o emissor de log.");

            return ActivatorUtilities.CreateInstance<LogPushSender>(sp);
        });

        services.AddSingleton<InMemoryPushQueue>();
        services.AddSingleton<IPushQueue>(sp => sp.GetRequiredService<InMemoryPushQueue>());
        services.AddSingleton<IOrderPushNotifications, OrderPushNotifications>();
        services.AddSingleton<IRecurringPushNotifications, RecurringPushNotifications>();
        services.AddScoped<PushDeliveryService>();
        services.AddScoped<IPushDeviceService, PushDeviceService>();
        services.AddHostedService<PushBackgroundService>();
        return services;
    }
}
