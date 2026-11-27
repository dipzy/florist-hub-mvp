using FloristHub.DAL.Constants;
using FloristHub.Services;
using FloristHub.Services.DTOs.Common;
using FloristHub.Services.DTOs.Florist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Florist)]
[Route("api/florist/customers")]
public class FloristCustomersController : ControllerBase
{
    private readonly IFloristCustomersService _customers;

    public FloristCustomersController(IFloristCustomersService customers) => _customers = customers;

    [HttpGet]
    public async Task<ActionResult<PagedResult<FloristCustomerDto>>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await _customers.ListAsync(search, page, pageSize, ct));
}
