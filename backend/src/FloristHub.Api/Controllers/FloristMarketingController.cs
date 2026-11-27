using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.Services.Marketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Florist)]
[Route("api/florist")]
public class FloristMarketingController : ControllerBase
{
    private readonly ICouponService _coupons;
    private readonly ICampaignService _campaigns;
    private readonly IReviewService _reviews;

    public FloristMarketingController(ICouponService coupons, ICampaignService campaigns, IReviewService reviews)
    {
        _coupons = coupons;
        _campaigns = campaigns;
        _reviews = reviews;
    }

    [HttpGet("coupons")]
    public async Task<ActionResult<IReadOnlyList<CouponDto>>> Coupons(CancellationToken ct) =>
        Ok(await _coupons.ListAsync(ct));

    [HttpPost("coupons")]
    public async Task<ActionResult<CouponDto>> CreateCoupon([FromBody] SaveCouponRequest request, CancellationToken ct) =>
        Ok(await _coupons.CreateAsync(request, User.GetUserId(), ct));

    [HttpPut("coupons/{id:guid}")]
    public async Task<ActionResult<CouponDto>> UpdateCoupon(Guid id, [FromBody] SaveCouponRequest request, CancellationToken ct) =>
        Ok(await _coupons.UpdateAsync(id, request, User.GetUserId(), ct));

    [HttpDelete("coupons/{id:guid}")]
    public async Task<IActionResult> DeleteCoupon(Guid id, CancellationToken ct)
    {
        await _coupons.DeleteAsync(id, User.GetUserId(), ct);
        return NoContent();
    }

    [HttpGet("campaigns")]
    public async Task<ActionResult<IReadOnlyList<CampaignDto>>> Campaigns(CancellationToken ct) =>
        Ok(await _campaigns.ListAsync(ct));

    [HttpPost("campaigns")]
    public async Task<ActionResult<CampaignDto>> CreateCampaign([FromBody] SaveCampaignRequest request, CancellationToken ct) =>
        Ok(await _campaigns.CreateAsync(request, User.GetUserId(), ct));

    [HttpPut("campaigns/{id:guid}")]
    public async Task<ActionResult<CampaignDto>> UpdateCampaign(Guid id, [FromBody] SaveCampaignRequest request, CancellationToken ct) =>
        Ok(await _campaigns.UpdateAsync(id, request, User.GetUserId(), ct));

    [HttpDelete("campaigns/{id:guid}")]
    public async Task<IActionResult> DeleteCampaign(Guid id, CancellationToken ct)
    {
        await _campaigns.DeleteAsync(id, User.GetUserId(), ct);
        return NoContent();
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<IReadOnlyList<OrderReviewDto>>> Reviews(CancellationToken ct) =>
        Ok(await _reviews.ListForFloristAsync(ct));

    [HttpPost("reviews/{id:guid}/reply")]
    public async Task<ActionResult<OrderReviewDto>> Reply(Guid id, [FromBody] ReplyReviewRequest request, CancellationToken ct) =>
        Ok(await _reviews.ReplyAsync(id, request.Reply, User.GetUserId(), ct));

    [HttpPost("reviews/{id:guid}/visibility")]
    public async Task<ActionResult<OrderReviewDto>> Visibility(Guid id, [FromBody] HideReviewRequest request, CancellationToken ct) =>
        Ok(await _reviews.SetHiddenAsync(id, request.Hidden, request.Reason, User.GetUserId(), ct));

    [HttpPost("reviews/{id:guid}/approve")]
    public async Task<ActionResult<OrderReviewDto>> Approve(Guid id, CancellationToken ct) =>
        Ok(await _reviews.ApproveAsync(id, User.GetUserId(), ct));
}
