using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.Services;
using FloristHub.Services.DTOs.Legal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Customer)]
[Route("api/privacy")]
public class PrivacyController : ControllerBase
{
    private readonly IDataSubjectRightsService _rights;

    public PrivacyController(IDataSubjectRightsService rights) => _rights = rights;

    [HttpGet("export")]
    public async Task<ActionResult<CustomerDataExportDto>> Export(CancellationToken ct) =>
        Ok(await _rights.ExportMyDataAsync(User.GetUserId(), ct));

    [HttpPost("anonymize")]
    public async Task<IActionResult> Anonymize(CancellationToken ct)
    {
        await _rights.AnonymizeMyAccountAsync(User.GetUserId(), ct);
        return NoContent();
    }
}
