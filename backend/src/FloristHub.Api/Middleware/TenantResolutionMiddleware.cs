using FloristHub.DAL.Constants;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FloristHub.Api.Middleware;

/// <summary>
/// Resolve o tenant por ordem: claim JWT, domínio próprio (host), subdomínio de App:BaseDomain, headers X-Tenant-*.
/// </summary>
public class TenantResolutionMiddleware
{
    public const string TenantIdHeader = "X-Tenant-Id";
    public const string TenantSlugHeader = "X-Tenant-Slug";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        if (TryResolveFromClaims(context, tenantContext) ||
            await TryResolveFromHostAsync(context, tenantContext) ||
            TryResolveFromHeaders(context, tenantContext))
        {
            // tenant definido
        }

        await _next(context);
    }

    private static bool TryResolveFromClaims(HttpContext context, TenantContext tenantContext)
    {
        if (context.User?.IsInRole(Roles.SuperAdmin) == true)
            return false;

        var claim = context.User?.FindFirst(CustomClaimTypes.TenantId)?.Value;
        var slug = context.User?.FindFirst(CustomClaimTypes.TenantSlug)?.Value;
        if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var tenantId))
            return false;
        tenantContext.SetTenant(tenantId, slug);
        return true;
    }

    private static bool TryResolveFromHeaders(HttpContext context, TenantContext tenantContext)
    {
        if (context.User?.Identity?.IsAuthenticated == true && context.User.IsInRole(Roles.SuperAdmin) != true)
            return false;

        if (context.Request.Headers.TryGetValue(TenantIdHeader, out var headerValue) &&
            Guid.TryParse(headerValue.FirstOrDefault(), out var tenantId))
        {
            var slug = context.Request.Headers[TenantSlugHeader].FirstOrDefault();
            tenantContext.SetTenant(tenantId, slug);
            return true;
        }

        return false;
    }

    private static async Task<bool> TryResolveFromHostAsync(HttpContext context, TenantContext tenantContext)
    {
        var host = context.Request.Host.Host;
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var baseDomain = configuration["App:BaseDomain"];

        using var scope = context.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (TenantHostResolution.IsCustomDomainHost(host, baseDomain))
        {
            var normalized = host.Trim().ToLowerInvariant();
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.CustomDomain == normalized);
            if (tenant is null)
                return false;
            tenantContext.SetTenant(tenant.Id, tenant.Slug);
            return true;
        }

        if (!TenantHostResolution.TryParseSubdomainSlug(host, baseDomain, out var slug))
            return false;

        var bySlug = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (bySlug is null)
            return false;

        tenantContext.SetTenant(bySlug.Id, bySlug.Slug);
        return true;
    }
}
