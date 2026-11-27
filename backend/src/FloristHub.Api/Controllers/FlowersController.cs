using FloristHub.Api.Extensions;
using FloristHub.Services.DTOs.Common;
using FloristHub.Services.DTOs.Flowers;
using FloristHub.Services;
using FloristHub.Services.Stock;
using FloristHub.DAL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FlowersController : ControllerBase
{
    private readonly IFlowerService _flowers;
    private readonly IStockService _stock;

    public FlowersController(IFlowerService flowers, IStockService stock)
    {
        _flowers = flowers;
        _stock = stock;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FlowerDto>>> List(CancellationToken ct) =>
        Ok(await _flowers.ListAsync(ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpGet("manage")]
    public async Task<ActionResult<PagedResult<FlowerDto>>> Manage(
        [FromQuery] string? search,
        [FromQuery] bool? catalogActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await _flowers.ListManageAsync(search, catalogActive, page, pageSize, ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPost]
    public async Task<ActionResult<FlowerDto>> Create([FromBody] CreateFlowerRequest request, CancellationToken ct) =>
        Ok(await _flowers.CreateAsync(request, User.GetUserId(), ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FlowerDto>> Update(Guid id, [FromBody] UpdateFlowerRequest request, CancellationToken ct) =>
        Ok(await _flowers.UpdateAsync(id, request, User.GetUserId(), ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpGet("{id:guid}/movements")]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> Movements(Guid id, CancellationToken ct) =>
        Ok(await _stock.ListMovementsAsync(id, ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpGet("alerts")]
    public async Task<ActionResult<IReadOnlyList<LowStockAlertDto>>> Alerts(CancellationToken ct) =>
        Ok(await _stock.ListLowStockAsync(ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _flowers.DeleteAsync(id, User.GetUserId(), ct);
        return NoContent();
    }
}
