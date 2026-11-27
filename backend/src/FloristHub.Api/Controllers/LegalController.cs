using FloristHub.DAL.Enums;
using FloristHub.Services;
using FloristHub.Services.DTOs.Legal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/legal")]
public class LegalController : ControllerBase
{
    private readonly ILegalComplianceService _legal;

    public LegalController(ILegalComplianceService legal) => _legal = legal;

    [HttpGet("versions")]
    public async Task<ActionResult<LegalVersionsDto>> Versions(CancellationToken ct) =>
        Ok(await _legal.GetCurrentVersionsAsync(ct));

    [HttpGet("terms")]
    public Task<ActionResult<PublicLegalDocumentDto>> Terms(CancellationToken ct) =>
        Document(LegalDocumentType.Terms, ct);

    [HttpGet("privacy")]
    public Task<ActionResult<PublicLegalDocumentDto>> Privacy(CancellationToken ct) =>
        Document(LegalDocumentType.Privacy, ct);

    [HttpGet("cookies")]
    public Task<ActionResult<PublicLegalDocumentDto>> Cookies(CancellationToken ct) =>
        Document(LegalDocumentType.Cookies, ct);

    private async Task<ActionResult<PublicLegalDocumentDto>> Document(LegalDocumentType type, CancellationToken ct)
    {
        try
        {
            return Ok(await _legal.GetDocumentAsync(type, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
