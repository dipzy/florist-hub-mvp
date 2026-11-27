using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.Services.Invoicing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Florist)]
[Route("api/florist/invoicing")]
public class FloristInvoicingController : ControllerBase
{
    private readonly IInvoiceService _invoices;

    public FloristInvoicingController(IInvoiceService invoices) => _invoices = invoices;

    [HttpGet]
    public async Task<ActionResult<InvoicingSettingsDto>> Get(CancellationToken ct) =>
        Ok(await _invoices.GetSettingsAsync(ct));

    [HttpPut]
    public async Task<ActionResult<InvoicingSettingsDto>> Update([FromBody] UpdateInvoicingSettingsRequest request, CancellationToken ct) =>
        Ok(await _invoices.UpdateSettingsAsync(request, User.GetUserId(), ct));
}

[ApiController]
[Authorize(Roles = $"{Roles.Florist},{Roles.Customer}")]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoices;

    public InvoicesController(IInvoiceService invoices) => _invoices = invoices;

    [Authorize(Roles = Roles.Florist)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalesDocumentDto>>> List(CancellationToken ct) =>
        Ok(await _invoices.ListForFloristAsync(ct));

    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SalesDocumentDto>>> ForOrder(Guid orderId, CancellationToken ct) =>
        Ok(await _invoices.ListForOrderAsync(orderId, User.GetUserId(), Role(), ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult<SalesDocumentDto>> Retry(Guid id, CancellationToken ct) =>
        Ok(await _invoices.RetryAsync(id, ct));

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var file = await _invoices.OpenPdfAsync(id, User.GetUserId(), Role(), ct);
        return File(file.Content, "application/pdf", file.FileName);
    }

    private string Role() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
}
