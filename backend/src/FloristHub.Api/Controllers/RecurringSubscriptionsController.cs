using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Enums;
using FloristHub.Services;
using FloristHub.Services.DTOs.Recurring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/recurring-subscriptions")]
public class RecurringSubscriptionsController : ControllerBase
{
    private readonly IRecurringSubscriptionService _recurring;

    public RecurringSubscriptionsController(IRecurringSubscriptionService recurring) => _recurring = recurring;

    [Authorize(Roles = Roles.Customer)]
    [HttpPost]
    public async Task<ActionResult<RecurringSubscriptionDto>> Create([FromBody] CreateRecurringSubscriptionRequest request, CancellationToken ct) =>
        Ok(await _recurring.CreateAsync(request, User.GetUserId(), ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<RecurringSubscriptionDto>>> Mine(CancellationToken ct) =>
        Ok(await _recurring.ListForCustomerAsync(User.GetUserId(), ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpGet("manage")]
    public async Task<ActionResult<IReadOnlyList<RecurringSubscriptionDto>>> Manage(
        [FromQuery] RecurringSubscriptionStatus? status,
        CancellationToken ct) =>
        Ok(await _recurring.ListForFloristAsync(status, ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpGet("upcoming")]
    public async Task<ActionResult<IReadOnlyList<FloristUpcomingRecurringDeliveryDto>>> Upcoming(
        [FromQuery] int days = 14,
        CancellationToken ct = default) =>
        Ok(await _recurring.ListUpcomingDeliveriesAsync(days, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecurringSubscriptionDto>> Get(Guid id, CancellationToken ct)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        return Ok(await _recurring.GetAsync(id, User.GetUserId(), role, ct));
    }

    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{id:guid}/pause")]
    public async Task<ActionResult<RecurringSubscriptionDto>> Pause(Guid id, CancellationToken ct) =>
        Ok(await _recurring.PauseAsync(id, User.GetUserId(), ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{id:guid}/resume")]
    public async Task<ActionResult<RecurringSubscriptionDto>> Resume(Guid id, CancellationToken ct) =>
        Ok(await _recurring.ResumeAsync(id, User.GetUserId(), ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{id:guid}/skip-next")]
    public async Task<ActionResult<RecurringSubscriptionDto>> SkipNext(Guid id, CancellationToken ct) =>
        Ok(await _recurring.SkipNextAsync(id, User.GetUserId(), ct));

    [Authorize(Roles = $"{Roles.Customer},{Roles.Florist}")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<RecurringSubscriptionDto>> Cancel(Guid id, CancellationToken ct)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        return Ok(await _recurring.CancelAsync(id, User.GetUserId(), role, ct));
    }

    [Authorize(Roles = Roles.Florist)]
    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<RecurringSubscriptionDto>> Accept(
        Guid id,
        [FromBody] FloristRecurringActionRequest request,
        CancellationToken ct) =>
        Ok(await _recurring.AcceptAsync(id, User.GetUserId(), request, ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<RecurringSubscriptionDto>> Reject(
        Guid id,
        [FromBody] FloristRecurringActionRequest request,
        CancellationToken ct) =>
        Ok(await _recurring.RejectAsync(id, User.GetUserId(), request, ct));
}
