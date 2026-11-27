using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.Services.Push;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{Roles.Florist},{Roles.Customer}")]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IPushDeviceService _devices;

    public NotificationsController(IPushDeviceService devices) => _devices = devices;

    [HttpPost("devices")]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest request, CancellationToken ct)
    {
        await _devices.RegisterAsync(User.GetUserId(), request.Token, request.Platform, ct);
        return NoContent();
    }

    [HttpDelete("devices")]
    public async Task<IActionResult> Unregister([FromBody] UnregisterDeviceRequest request, CancellationToken ct)
    {
        await _devices.UnregisterAsync(User.GetUserId(), request.Token, ct);
        return NoContent();
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferencesDto>> GetPreferences(CancellationToken ct) =>
        Ok(await _devices.GetPreferencesAsync(User.GetUserId(), ct));

    [HttpPut("preferences")]
    public async Task<ActionResult<NotificationPreferencesDto>> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken ct) =>
        Ok(await _devices.UpdatePreferencesAsync(User.GetUserId(), request, ct));
}

public sealed record RegisterDeviceRequest(string Token, string Platform);

public sealed record UnregisterDeviceRequest(string Token);
