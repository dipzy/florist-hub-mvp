using FloristHub.DAL.Repositories.Interfaces;
using FloristHub.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FloristHub.Api.Configuration;

/// <summary>Permite origens estáticas (config) e domínios próprios registados nas lojas.</summary>
public class TenantCorsOriginChecker : ICorsDomainCacheInvalidator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string[] _staticOrigins;
    private readonly string? _baseDomain;
    private readonly object _gate = new();
    private HashSet<string> _customHosts = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset _expires = DateTimeOffset.MinValue;

    public TenantCorsOriginChecker(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _staticOrigins = CorsOrigins.Resolve(configuration);
        _baseDomain = configuration["App:BaseDomain"];
    }

    public bool IsAllowed(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return false;

        if (_staticOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            return true;

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();
        if (host is "localhost" or "127.0.0.1")
            return true;

        if (uri.Scheme == "https" && CorsOrigins.IsPlatformHost(host, _baseDomain))
            return true;

        RefreshCacheIfStale();
        lock (_gate)
            return _customHosts.Contains(host);
    }

    public void Invalidate() => InvalidateCache();

    public void InvalidateCache()
    {
        lock (_gate)
            _expires = DateTimeOffset.MinValue;
    }

    private void RefreshCacheIfStale()
    {
        lock (_gate)
        {
            if (_expires > DateTimeOffset.UtcNow)
                return;
        }

        using var scope = _scopeFactory.CreateScope();
        var tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var domains = tenants.ListCustomDomainsReadOnlyAsync().GetAwaiter().GetResult();
        var hosts = domains
            .Select(d => d.Trim().ToLowerInvariant())
            .Where(d => !string.IsNullOrEmpty(d))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        lock (_gate)
        {
            _customHosts = hosts;
            _expires = DateTimeOffset.UtcNow.AddMinutes(5);
        }
    }
}
