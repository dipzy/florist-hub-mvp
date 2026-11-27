using FloristHub.Api.Extensions;
using FloristHub.DAL.MultiTenancy;
using FloristHub.Services.Interfaces.Storage;
using FloristHub.DAL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private readonly IFileStorage _files;
    private readonly ITenantContext _tenant;

    public FilesController(IFileStorage files, ITenantContext tenant)
    {
        _files = files;
        _tenant = tenant;
    }

    [Authorize(Roles = Roles.Florist + "," + Roles.Customer)]
    [HttpPost("upload")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<PortfolioPhotoUploadResponse>> Upload(IFormFile file, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? User.GetTenantId();
        if (tenantId == Guid.Empty)
            return BadRequest("Tenant não resolvido.");

        if (file.Length == 0 || file.Length > MaxUploadBytes)
            return BadRequest("Ficheiro inválido ou demasiado grande (máx. 5 MB).");

        var contentType = file.ContentType ?? "application/octet-stream";
        if (!AllowedContentTypes.Contains(contentType))
            return BadRequest("Tipo de ficheiro não permitido. Use JPEG, PNG, WebP ou GIF.");

        await using var stream = file.OpenReadStream();
        var stored = await _files.SaveAsync(stream, contentType, file.FileName, tenantId, ct);
        return Ok(new PortfolioPhotoUploadResponse(stored.PublicUrl, stored.StoragePath, stored.ContentType, stored.SizeBytes));
    }

    [AllowAnonymous]
    [HttpGet("{*storagePath}")]
    public async Task<IActionResult> Download(string storagePath, CancellationToken ct)
    {
        if (storagePath.Contains("..", StringComparison.Ordinal))
            return BadRequest();

        if (!IsPublicDemoPath(storagePath) && TryParseTenantFolder(storagePath, out var pathTenantId))
        {
            var resolved = _tenant.TenantId;
            if (!resolved.HasValue || resolved.Value != pathTenantId)
                return NotFound();
        }

        var stream = await _files.OpenReadAsync(storagePath, ct);
        if (stream is null)
            return NotFound();

        var contentType = storagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
            : storagePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp"
            : storagePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ? "image/gif"
            : "image/jpeg";

        return File(stream, contentType, enableRangeProcessing: true);
    }

    private static bool IsPublicDemoPath(string storagePath) =>
        storagePath.StartsWith("demo/", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseTenantFolder(string storagePath, out Guid tenantId)
    {
        tenantId = default;
        var slash = storagePath.IndexOf('/');
        if (slash <= 0)
            return false;
        return Guid.TryParseExact(storagePath[..slash], "N", out tenantId);
    }
}

public record PortfolioPhotoUploadResponse(string Url, string StoragePath, string ContentType, long SizeBytes);
