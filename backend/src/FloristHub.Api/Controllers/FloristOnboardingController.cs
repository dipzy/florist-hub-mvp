using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.Services.DTOs.Onboarding;
using FloristHub.Services.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Florist)]
[Route("api/florist/onboarding")]
public class FloristOnboardingController : ControllerBase
{
    private readonly IFloristOnboardingService _onboarding;

    public FloristOnboardingController(IFloristOnboardingService onboarding) => _onboarding = onboarding;

    [HttpGet]
    public async Task<ActionResult<FloristOnboardingStatusDto>> Get(CancellationToken ct) =>
        Ok(await _onboarding.GetStatusAsync(ct));

    [HttpPut("wizard-step")]
    public async Task<ActionResult<FloristOnboardingStatusDto>> SetWizardStep(
        [FromBody] SetWizardStepRequest request,
        CancellationToken ct) =>
        Ok(await _onboarding.SetWizardStepAsync(request.Step, User.GetUserId(), ct));

    [HttpPost("acknowledge-colors")]
    public async Task<ActionResult<FloristOnboardingStatusDto>> AcknowledgeColors(CancellationToken ct) =>
        Ok(await _onboarding.AcknowledgeColorsAsync(User.GetUserId(), ct));

    [HttpPut("daily-capacity")]
    public async Task<ActionResult<FloristOnboardingStatusDto>> SetDailyCapacity(
        [FromBody] SetDailyCapacityRequest request,
        CancellationToken ct) =>
        Ok(await _onboarding.SetDailyCapacityAsync(request.DailyOrderLimit, User.GetUserId(), ct));

    [HttpPut("payments-disabled")]
    public async Task<ActionResult<FloristOnboardingStatusDto>> SetPaymentsDisabled(
        [FromBody] SetPaymentsDisabledByChoiceRequest request,
        CancellationToken ct) =>
        Ok(await _onboarding.SetPaymentsDisabledByChoiceAsync(request.Disabled, User.GetUserId(), ct));

    [HttpPost("accept-platform-terms")]
    public async Task<ActionResult<FloristOnboardingStatusDto>> AcceptPlatformTerms(CancellationToken ct) =>
        Ok(await _onboarding.AcceptPlatformTermsAsync(User.GetUserId(), ct));

    [HttpPost("publish")]
    public async Task<ActionResult<FloristOnboardingStatusDto>> Publish(CancellationToken ct) =>
        Ok(await _onboarding.PublishAsync(User.GetUserId(), ct));
}
