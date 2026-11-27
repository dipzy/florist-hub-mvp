using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.Services.DTOs.Payments;
using FloristHub.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.SuperAdmin)]
[Route("api/platform")]
public class PlatformBillingController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PlatformBillingController(IPaymentService payments) => _payments = payments;

    [HttpGet("billing")]
    public async Task<ActionResult<PlatformBillingDto>> Get(CancellationToken ct) =>
        Ok(await _payments.GetPlatformBillingAsync(ct));

    [HttpPut("billing")]
    public async Task<ActionResult<PlatformBillingDto>> Update(
        [FromBody] UpdatePlatformBillingRequest request,
        CancellationToken ct) =>
        Ok(await _payments.UpdatePlatformBillingAsync(request.CommissionPercent, request.MonthlyFeeAmount, User.GetUserId(), ct));

    [HttpGet("tenants/{id:guid}/billing")]
    public async Task<ActionResult<TenantBillingDto>> GetTenant(Guid id, CancellationToken ct) =>
        Ok(await _payments.GetTenantBillingAsync(id, ct));

    [HttpPut("tenants/{id:guid}/billing")]
    public async Task<ActionResult<TenantBillingDto>> UpdateTenant(
        Guid id,
        [FromBody] UpdateTenantBillingRequest request,
        CancellationToken ct) =>
        Ok(await _payments.UpdateTenantBillingAsync(id, request.CommissionPercentOverride, request.MonthlyFeeAmount, User.GetUserId(), ct));
}
