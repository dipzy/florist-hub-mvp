using FloristHub.DAL.Constants;
using FloristHub.Services;
using FloristHub.Services.DTOs.Common;
using FloristHub.Services.DTOs.Legal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/compliance-audit")]
public class ComplianceAuditController : ControllerBase
{
    private readonly IComplianceAuditService _audit;

    public ComplianceAuditController(IComplianceAuditService audit) => _audit = audit;

    [Authorize(Roles = Roles.Florist)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ComplianceAuditEntryDto>>> ForFlorist(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        Ok(await _audit.ListForFloristAsync(page, pageSize, ct));

    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpGet("platform")]
    public async Task<ActionResult<PagedResult<ComplianceAuditEntryDto>>> ForPlatform(
        [FromQuery] Guid? tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        Ok(await _audit.ListForSuperAdminAsync(page, pageSize, tenantId, ct));
}
