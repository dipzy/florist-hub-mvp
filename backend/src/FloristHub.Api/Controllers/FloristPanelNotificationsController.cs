using FloristHub.Api.Auth;
using FloristHub.DAL.Constants;
using FloristHub.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Florist)]
[Route("api/florist/panel-notifications")]
public class FloristPanelNotificationsController : ControllerBase
{
    private readonly IFloristPanelNotificationService _notifications;

    public FloristPanelNotificationsController(IFloristPanelNotificationService notifications) => _notifications = notifications;

    [HttpGet("summary")]
    public async Task<ActionResult<FloristPanelNotificationSummaryDto>> Summary(CancellationToken ct) =>
        Ok(await _notifications.GetSummaryAsync(ct));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FloristPanelNotificationDto>>> List([FromQuery] int limit = 20, CancellationToken ct = default) =>
        Ok(await _notifications.ListUnreadAsync(limit, ct));

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkRead(CancellationToken ct)
    {
        await _notifications.MarkAllReadAsync(ct);
        return NoContent();
    }
}
