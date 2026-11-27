using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.Services.DTOs.Payments;
using FloristHub.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Florist)]
[Route("api/florist")]
public class FloristPaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public FloristPaymentsController(IPaymentService payments) => _payments = payments;

    [HttpGet("stripe")]
    public async Task<ActionResult<StripeConnectStatusDto>> StripeStatus(CancellationToken ct) =>
        Ok(await _payments.GetConnectStatusAsync(ct));

    [HttpPost("stripe/onboarding")]
    public async Task<ActionResult<StripeOnboardingResponse>> Onboarding(
        [FromBody] StripeOnboardingRequest request,
        CancellationToken ct) =>
        Ok(await _payments.StartOnboardingAsync(request.ReturnUrl, request.RefreshUrl, ct));

    [HttpGet("payments")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> List(CancellationToken ct) =>
        Ok(await _payments.ListForFloristAsync(ct));

    [HttpPost("payments/{id:guid}/refund")]
    public async Task<ActionResult<PaymentDto>> Refund(
        Guid id,
        [FromBody] RefundPaymentRequest request,
        CancellationToken ct) =>
        Ok(await _payments.RefundAsync(id, request.Amount, request.Reason, User.GetUserId(), ct));
}
