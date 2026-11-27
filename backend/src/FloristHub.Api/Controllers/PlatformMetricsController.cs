using FloristHub.DAL.Constants;
using FloristHub.Services;
using FloristHub.Services.DTOs.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.SuperAdmin)]
[Route("api/platform/metrics")]
public class PlatformMetricsController : ControllerBase
{
    private readonly IPlatformMetricsService _metrics;

    public PlatformMetricsController(IPlatformMetricsService metrics) => _metrics = metrics;

    [HttpGet]
    public async Task<ActionResult<PlatformMetricsDto>> Get([FromQuery] int days = 30, CancellationToken ct = default) =>
        Ok(await _metrics.GetAsync(days, ct));
}
