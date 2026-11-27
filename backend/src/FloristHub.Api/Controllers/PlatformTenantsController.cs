using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.Services;
using FloristHub.Services.DTOs.Onboarding;
using FloristHub.Services.DTOs.Platform;
using FloristHub.Services.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.SuperAdmin)]
[Route("api/platform/tenants")]
public class PlatformTenantsController : ControllerBase
{
    private readonly IPlatformTenantService _platform;
    private readonly IFloristOnboardingService _onboarding;

    public PlatformTenantsController(IPlatformTenantService platform, IFloristOnboardingService onboarding)
    {
        _platform = platform;
        _onboarding = onboarding;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlatformTenantListItemDto>>> List(CancellationToken ct) =>
        Ok(await _platform.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlatformTenantDetailDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _platform.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CreatePlatformTenantResponse>> Create(
        [FromBody] CreatePlatformTenantRequest request,
        CancellationToken ct) =>
        Ok(await _platform.CreateAsync(request, User.GetUserId(), ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PlatformTenantDetailDto>> Update(
        Guid id,
        [FromBody] UpdatePlatformTenantRequest request,
        CancellationToken ct) =>
        Ok(await _platform.UpdateAsync(id, request, User.GetUserId(), ct));

    [HttpPost("{id:guid}/suspend")]
    public async Task<ActionResult<PlatformTenantDetailDto>> Suspend(
        Guid id,
        [FromBody] SuspendPlatformTenantRequest request,
        CancellationToken ct) =>
        Ok(await _platform.SetSuspendedAsync(id, request.Suspended, User.GetUserId(), ct));

    [HttpPost("{id:guid}/resend-invite")]
    public async Task<ActionResult<ResendFloristInviteResponse>> ResendInvite(Guid id, CancellationToken ct) =>
        Ok(await _platform.ResendFloristInviteAsync(id, User.GetUserId(), ct));

    [HttpGet("{id:guid}/onboarding")]
    public async Task<ActionResult<PlatformTenantOnboardingDto>> Onboarding(Guid id, CancellationToken ct) =>
        Ok(await _onboarding.GetPlatformViewAsync(id, ct));
}
