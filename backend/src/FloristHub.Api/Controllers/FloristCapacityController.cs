using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.Services.Capacity;
using FloristHub.Services.DTOs.Capacity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Florist)]
[Route("api/florist/capacity")]
public class FloristCapacityController : ControllerBase
{
    private readonly IDeliveryCapacityService _capacity;

    public FloristCapacityController(IDeliveryCapacityService capacity) => _capacity = capacity;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FloristCapacityDayDto>>> Calendar(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct) =>
        Ok(await _capacity.GetFloristCalendarAsync(from, to, ct));

    [HttpGet("settings")]
    public async Task<ActionResult<DeliveryCapacitySettingsDto>> Settings(CancellationToken ct) =>
        Ok(await _capacity.GetSettingsAsync(ct));

    [HttpPut("settings")]
    public async Task<ActionResult<DeliveryCapacitySettingsDto>> UpdateSettings(
        [FromBody] UpdateDeliveryCapacitySettingsRequest request,
        CancellationToken ct) =>
        Ok(await _capacity.UpdateSettingsAsync(request, User.GetUserId(), ct));

    [HttpPut("exceptions")]
    public async Task<ActionResult<DeliveryExceptionDto>> UpsertException(
        [FromBody] UpsertDeliveryCapacityExceptionRequest request,
        CancellationToken ct) =>
        Ok(await _capacity.UpsertExceptionAsync(request, User.GetUserId(), ct));

    [HttpDelete("exceptions/{date}")]
    public async Task<IActionResult> DeleteException(DateOnly date, CancellationToken ct)
    {
        await _capacity.DeleteExceptionAsync(date, User.GetUserId(), ct);
        return NoContent();
    }
}
