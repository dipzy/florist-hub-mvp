using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Marketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

/// <summary>
/// HTML, robots e sitemap por loja. A SPA não mete meta tags no primeiro HTML;
/// crawlers pedem estes endpoints (ou o proxy encaminha /robots.txt e /sitemap.xml para aqui).
/// </summary>
[ApiController]
[AllowAnonymous]
public class PublicSeoController : ControllerBase
{
    private readonly ISeoService _seo;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public PublicSeoController(ISeoService seo, ITenantContext tenant, IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _seo = seo;
        _tenant = tenant;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    [HttpGet("/robots.txt")]
    [HttpGet("/api/seo/robots.txt")]
    public async Task<ContentResult> Robots(CancellationToken ct)
    {
        var tenant = await RequireTenantAsync(ct);
        var basis = _seo.ResolvePublicBase(Request.Scheme, Request.Host.Value ?? "localhost", _configuration["App:BaseDomain"], tenant);
        return Content(await _seo.RobotsAsync(basis, ct), "text/plain; charset=utf-8");
    }

    [HttpGet("/sitemap.xml")]
    [HttpGet("/api/seo/sitemap.xml")]
    public async Task<ContentResult> Sitemap(CancellationToken ct)
    {
        var tenant = await RequireTenantAsync(ct);
        var basis = _seo.ResolvePublicBase(Request.Scheme, Request.Host.Value ?? "localhost", _configuration["App:BaseDomain"], tenant);
        return Content(await _seo.SitemapAsync(basis, ct), "application/xml; charset=utf-8");
    }

    [HttpGet("/api/seo/documento")]
    public async Task<ContentResult> Document([FromQuery] string? caminho, CancellationToken ct)
    {
        var tenant = await RequireTenantAsync(ct);
        var basis = _seo.ResolvePublicBase(Request.Scheme, Request.Host.Value ?? "localhost", _configuration["App:BaseDomain"], tenant);
        var assetBase = $"{Request.Scheme}://{Request.Host.Value}";
        var html = await _seo.DocumentAsync(basis, caminho, assetBase, ct);
        return Content(html, "text/html; charset=utf-8");
    }

    private async Task<FloristHub.DAL.Entities.Tenant> RequireTenantAsync(CancellationToken ct)
    {
        var id = _tenant.TenantId ?? throw new InvalidOperationException("Loja não resolvida.");
        return await _unitOfWork.Tenants.GetByIdReadOnlyAsync(id, ct)
            ?? throw new InvalidOperationException("Loja não encontrada.");
    }
}
