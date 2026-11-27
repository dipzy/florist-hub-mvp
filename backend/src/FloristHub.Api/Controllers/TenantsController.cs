using FloristHub.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantRepository _tenants;

    public TenantsController(ITenantRepository tenants) => _tenants = tenants;

    /// <summary>Directório público de lojas desativado — evita descoberta entre tenants.</summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult List() => NotFound();

    /// <summary>Resolve uma loja activa pelo slug (subdomínio, ?tenant=, etc.) sem listar outras.</summary>
    [AllowAnonymous]
    [HttpGet("resolve/{slug}")]
    public async Task<ActionResult<object>> ResolveBySlug(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var normalized = slug.Trim().ToLowerInvariant();
        var tenant = await _tenants.GetBySlugReadOnlyAsync(normalized, ct);
        if (tenant is null || !tenant.IsActive)
            return NotFound();

        return Ok(new { tenant.Id, tenant.Name, tenant.Slug, tenant.Description });
    }
}
