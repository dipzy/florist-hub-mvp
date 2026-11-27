using FloristHub.Api.Extensions;
using FloristHub.DAL.Enums;
using FloristHub.Services.DTOs.Portfolio;
using FloristHub.Services;
using FloristHub.DAL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolio;

    public PortfolioController(IPortfolioService portfolio) => _portfolio = portfolio;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PortfolioArrangementDto>>> ListActive(CancellationToken ct) =>
        Ok(await _portfolio.ListActiveAsync(ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpGet("manage")]
    public async Task<ActionResult<IReadOnlyList<PortfolioArrangementDto>>> ListForFlorist(CancellationToken ct) =>
        Ok(await _portfolio.ListForFloristAsync(ct));

    [AllowAnonymous]
    [HttpGet("por/{slug}")]
    public async Task<ActionResult<PortfolioArrangementDto>> GetBySlug(string slug, CancellationToken ct) =>
        Ok(await _portfolio.GetBySlugAsync(slug, ct));

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PortfolioArrangementDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _portfolio.GetByIdAsync(id, ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPost]
    public async Task<ActionResult<PortfolioArrangementDto>> Create([FromBody] CreatePortfolioArrangementRequest request, CancellationToken ct) =>
        Ok(await _portfolio.CreateAsync(request, User.GetUserId(), ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PortfolioArrangementDto>> Update(Guid id, [FromBody] UpdatePortfolioArrangementRequest request, CancellationToken ct) =>
        Ok(await _portfolio.UpdateAsync(id, request, User.GetUserId(), ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<PortfolioArrangementDto>> SetStatus(
        Guid id,
        [FromBody] PortfolioStatusRequest request,
        CancellationToken ct) =>
        Ok(await _portfolio.SetStatusAsync(id, request.Status, User.GetUserId(), ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPut("sort-order")]
    public async Task<ActionResult<IReadOnlyList<PortfolioArrangementDto>>> UpdateSortOrder(
        [FromBody] UpdatePortfolioSortOrdersRequest request,
        CancellationToken ct) =>
        Ok(await _portfolio.UpdateSortOrdersAsync(request, User.GetUserId(), ct));
}

public record PortfolioStatusRequest(PortfolioArrangementStatus Status);
