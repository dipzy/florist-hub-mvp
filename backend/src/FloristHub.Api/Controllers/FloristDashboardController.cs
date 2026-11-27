using FloristHub.DAL.Constants;
using FloristHub.Services;
using FloristHub.Services.DTOs.Florist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Florist)]
[Route("api/florist/dashboard")]
public class FloristDashboardController : ControllerBase
{
    private readonly IFloristDashboardService _dashboard;

    public FloristDashboardController(IFloristDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet]
    public async Task<ActionResult<FloristDashboardDto>> Get(CancellationToken ct) =>
        Ok(await _dashboard.GetAsync(ct));

    [HttpGet("metrics")]
    public async Task<ActionResult<FloristDashboardMetricsDto>> GetMetrics(
        [FromQuery] int days = 30,
        CancellationToken ct = default) =>
        Ok(await _dashboard.GetMetricsAsync(days, ct));
}
