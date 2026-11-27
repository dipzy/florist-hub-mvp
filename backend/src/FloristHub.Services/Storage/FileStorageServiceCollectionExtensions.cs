using FloristHub.Services.Interfaces.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FloristHub.Services.Storage;

public static class FileStorageServiceCollectionExtensions
{
    public static IServiceCollection AddFloristHubFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Storage:Provider"]
            ?? configuration["FileStorage:Provider"]
            ?? "Local";

        if (provider.Equals("S3", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("R2", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IFileStorage, S3ObjectFileStorage>();
        else
            services.AddSingleton<IFileStorage, LocalDiskFileStorage>();

        return services;
    }
}
