using Microsoft.Extensions.Configuration;

namespace FloristHub.Api.Configuration;

public static class CorsOrigins
{
    public static string[] Resolve(IConfiguration configuration)
    {
        var allowed = configuration["Cors:AllowedOrigins"];
        if (!string.IsNullOrWhiteSpace(allowed))
            return allowed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var array = configuration.GetSection("Cors:Origins").Get<string[]>();
        if (array is { Length: > 0 })
            return array;

        return ["http://localhost:5173", "http://localhost:4173"];
    }

    public static bool IsPlatformHost(string host, string? baseDomain)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(baseDomain))
            return false;

        var normalized = baseDomain.Trim().Trim('.').ToLowerInvariant();
        host = host.Trim().ToLowerInvariant();
        return host == normalized || host.EndsWith("." + normalized, StringComparison.Ordinal);
    }
}
