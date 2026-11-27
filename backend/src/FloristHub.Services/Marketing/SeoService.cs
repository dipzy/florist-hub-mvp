using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Onboarding;
using FloristHub.Services.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Marketing;

public interface ISeoService
{
    Task<string> RobotsAsync(string publicBase, CancellationToken ct = default);
    Task<string> SitemapAsync(string publicBase, CancellationToken ct = default);
    Task<string> DocumentAsync(string publicBase, string? path, string? assetBase, CancellationToken ct = default);
    string ResolvePublicBase(string requestScheme, string requestHost, string? baseDomain, Tenant tenant);
}

public sealed class SeoService : ISeoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IReviewService _reviews;

    public SeoService(IUnitOfWork unitOfWork, ITenantContext tenant, IReviewService reviews)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _reviews = reviews;
    }

    public Task<string> RobotsAsync(string publicBase, CancellationToken ct = default) =>
        Task.FromResult(SeoMarkup.Robots(Combine(publicBase, "/sitemap.xml")));

    public async Task<string> SitemapAsync(string publicBase, CancellationToken ct = default)
    {
        await RequireTenantAsync(ct);
        var urls = new List<string>
        {
            Combine(publicBase, "/"),
            Combine(publicBase, "/portfolio"),
            Combine(publicBase, "/flores-da-epoca")
        };
        var arrangements = await _unitOfWork.Portfolio.Query()
            .Where(p => p.Status == PortfolioArrangementStatus.Active && p.Slug != null && p.Slug != "")
            .Select(p => p.Slug!)
            .ToListAsync(ct);
        urls.AddRange(arrangements.Select(slug => Combine(publicBase, "/portfolio/" + slug)));
        var today = LisbonClock.Today();
        var campaigns = await _unitOfWork.Campaigns.Query()
            .Where(c => c.IsActive && c.StartsOn <= today && c.EndsOn >= today)
            .Select(c => c.Slug)
            .ToListAsync(ct);
        urls.AddRange(campaigns.Select(slug => Combine(publicBase, "/campanha/" + slug)));
        return SeoMarkup.Sitemap(urls);
    }

    public async Task<string> DocumentAsync(string publicBase, string? path, string? assetBase, CancellationToken ct = default)
    {
        var tenant = await RequireTenantAsync(ct);
        var summary = await _reviews.SummaryAsync(ct);
        var route = Normalize(path);
        var title = tenant.SeoTitle?.Trim();
        var description = tenant.SeoDescription?.Trim();
        string? image = Absolute(assetBase ?? publicBase, tenant.LogoUrl);

        if (route == "/" || route == "/loja")
        {
            title ??= tenant.Name + " — florista";
            description ??= tenant.Description ?? "Encomende flores e arranjos nesta florista.";
        }
        else if (route == "/portfolio")
        {
            title = "Portfolio — " + tenant.Name;
            description ??= "Arranjos reais desta florista.";
        }
        else if (route.StartsWith("/portfolio/", StringComparison.Ordinal))
        {
            var slug = route["/portfolio/".Length..];
            var arrangement = await _unitOfWork.Portfolio.QueryWithDetails()
                .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PortfolioArrangementStatus.Active, ct)
                ?? throw new KeyNotFoundException("Página não encontrada.");
            title = arrangement.Title + " — " + tenant.Name;
            description = arrangement.Description ?? description ?? arrangement.Title;
            image = Absolute(assetBase ?? publicBase, arrangement.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault()) ?? image;
        }
        else if (route == "/flores-da-epoca")
        {
            title = "Flores da época — " + tenant.Name;
            description ??= "Flores de época disponíveis nesta loja.";
        }
        else if (route.StartsWith("/campanha/", StringComparison.Ordinal))
        {
            var slug = route["/campanha/".Length..];
            var today = LisbonClock.Today();
            var campaign = await _unitOfWork.Campaigns.Query()
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive && c.StartsOn <= today && c.EndsOn >= today, ct)
                ?? throw new KeyNotFoundException("Página não encontrada.");
            title = campaign.Name + " — " + tenant.Name;
            description = campaign.Description ?? description ?? campaign.Name;
        }
        else
        {
            throw new KeyNotFoundException("Página não encontrada.");
        }

        title ??= tenant.Name;
        description ??= tenant.Name;
        var canonical = Combine(publicBase, route == "/loja" ? "/" : route);
        var json = SeoMarkup.JsonLdFlorist(
            tenant.Name,
            description,
            Combine(publicBase, "/"),
            image,
            tenant.AddressLine,
            tenant.City,
            tenant.PostalCode,
            summary.ReviewCount > 0 ? summary.AverageRating : null,
            summary.ReviewCount);
        return SeoMarkup.Html(new SeoPage(title, description, canonical, image, tenant.Name, json));
    }

    public string ResolvePublicBase(string requestScheme, string requestHost, string? baseDomain, Tenant tenant)
    {
        var host = requestHost.Split(':')[0];
        if (TenantHostResolution.TryParseSubdomainSlug(host, baseDomain, out var slug) &&
            string.Equals(slug, tenant.Slug, StringComparison.OrdinalIgnoreCase))
            return requestScheme + "://" + requestHost;
        if (!string.IsNullOrWhiteSpace(tenant.CustomDomain) &&
            string.Equals(host, tenant.CustomDomain, StringComparison.OrdinalIgnoreCase))
            return requestScheme + "://" + requestHost;
        if (!string.IsNullOrWhiteSpace(tenant.CustomDomain))
            return "https://" + tenant.CustomDomain;
        if (!string.IsNullOrWhiteSpace(baseDomain))
            return "https://" + tenant.Slug + "." + baseDomain.Trim().Trim('.');
        return requestScheme + "://" + requestHost;
    }

    private Task<Tenant> RequireTenantAsync(CancellationToken ct) =>
        TenantPublicAccessRules.RequirePublishedTenantAsync(_tenant, _unitOfWork, ct);

    private static string Normalize(string? path)
    {
        var value = string.IsNullOrWhiteSpace(path) ? "/" : path.Trim();
        if (!value.StartsWith('/'))
            value = "/" + value;
        var query = value.IndexOf('?', StringComparison.Ordinal);
        if (query >= 0)
            value = value[..query];
        if (value.Length > 1 && value.EndsWith('/'))
            value = value.TrimEnd('/');
        return value.ToLowerInvariant();
    }

    private static string Combine(string baseUrl, string path)
    {
        var root = baseUrl.TrimEnd('/');
        return path == "/" ? root + "/" : root + path;
    }

    private static string? Absolute(string baseUrl, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;
        if (url.StartsWith('/'))
            return baseUrl.TrimEnd('/') + url;
        return null;
    }
}
