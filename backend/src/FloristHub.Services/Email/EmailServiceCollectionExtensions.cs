using FloristHub.Services.Interfaces.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FloristHub.Services.Email;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddFloristHubEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.PostConfigure<EmailOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.WebBaseUrl))
                options.WebBaseUrl = configuration["App:WebBaseUrl"];
        });

        var options = configuration.GetSection(EmailOptions.SectionName);
        var provider = options["Provider"] ?? "Log";
        var apiKey = options["ResendApiKey"];
        var useResend = provider.Equals("Resend", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(apiKey);

        services.AddHttpClient(ResendEmailSender.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        });

        if (useResend)
        {
            services.AddSingleton<IEmailSender>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var http = factory.CreateClient(ResendEmailSender.HttpClientName);
                return ActivatorUtilities.CreateInstance<ResendEmailSender>(sp, http);
            });
        }
        else
        {
            services.AddSingleton<IEmailSender, LogEmailSender>();
        }

        services.AddSingleton<InMemoryEmailQueue>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<InMemoryEmailQueue>());
        services.AddHostedService<EmailBackgroundService>();

        services.AddSingleton<IOrderEmailNotifications, OrderEmailNotificationsService>();
        services.AddSingleton<IAuthEmailNotifications, AuthEmailNotificationsService>();
        services.AddSingleton<IRecurringEmailNotifications, RecurringEmailNotificationsService>();

        return services;
    }
}
