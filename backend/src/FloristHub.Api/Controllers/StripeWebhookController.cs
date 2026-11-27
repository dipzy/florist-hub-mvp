using FloristHub.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly IStripeGateway _stripe;

    public StripeWebhookController(IPaymentService payments, IStripeGateway stripe)
    {
        _payments = payments;
        _stripe = stripe;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        if (!_stripe.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Pagamentos Stripe desativados." });

        string payload;
        using (var reader = new StreamReader(Request.Body))
            payload = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["Stripe-Signature"].ToString();
        try
        {
            await _payments.HandleWebhookAsync(payload, signature, ct);
            return Ok(new { received = true });
        }
        catch (StripeSignatureException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
