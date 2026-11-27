namespace FloristHub.Services.Tenancy;

/// <summary>Resolução do tenant a partir do host HTTP (domínio próprio ou subdomínio).</summary>
public static class TenantHostResolution
{
    /// <summary>Hosts da plataforma (Pages, Render, localhost) nunca são domínio de uma loja.</summary>
    public static bool IsPlatformHostname(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return true;

        host = host.Trim().TrimEnd('.').ToLowerInvariant();
        if (host is "localhost" or "127.0.0.1" or "::1" or "[::1]")
            return true;

        string[] suffixes = [".pages.dev", ".workers.dev", ".onrender.com", ".localhost", ".local"];
        foreach (var suffix in suffixes)
        {
            var bare = suffix.TrimStart('.');
            if (host == bare || host.EndsWith(suffix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static bool TryParseSubdomainSlug(string host, string? baseDomain, out string slug)
    {
        slug = string.Empty;
        if (string.IsNullOrWhiteSpace(host) || IsPlatformHostname(host))
            return false;

        host = host.Trim().ToLowerInvariant();
        var normalizedBase = NormalizeHost(baseDomain);
        if (string.IsNullOrEmpty(normalizedBase) || IsPlatformHostname(normalizedBase))
            return false;

        if (host == normalizedBase || host == "www." + normalizedBase)
            return false;

        var suffix = "." + normalizedBase;
        if (!host.EndsWith(suffix, StringComparison.Ordinal))
            return false;

        var prefix = host[..^suffix.Length];
        if (string.IsNullOrEmpty(prefix) || prefix.Contains('.') || prefix.Equals("www", StringComparison.Ordinal))
            return false;

        slug = prefix;
        return true;
    }

    public static bool IsCustomDomainHost(string host, string? baseDomain)
    {
        if (string.IsNullOrWhiteSpace(host) || IsPlatformHostname(host))
            return false;
        host = host.Trim().ToLowerInvariant();
        var normalizedBase = NormalizeHost(baseDomain);
        if (string.IsNullOrEmpty(normalizedBase))
            return !host.Contains("localhost", StringComparison.Ordinal);

        if (host == normalizedBase || host.EndsWith("." + normalizedBase, StringComparison.Ordinal))
            return false;

        return !host.EndsWith(".localhost", StringComparison.Ordinal)
               && host != "localhost"
               && !host.EndsWith(".local", StringComparison.Ordinal);
    }

    public static string? NormalizeCustomDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var v = value.Trim().ToLowerInvariant();
        if (v.StartsWith("http://", StringComparison.Ordinal))
            v = v["http://".Length..];
        else if (v.StartsWith("https://", StringComparison.Ordinal))
            v = v["https://".Length..];
        var slash = v.IndexOf('/');
        if (slash >= 0)
            v = v[..slash];
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    private static string? NormalizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;
        return host.Trim().ToLowerInvariant();
    }
}
