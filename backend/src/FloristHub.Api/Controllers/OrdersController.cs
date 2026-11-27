using FloristHub.Api.Extensions;
using FloristHub.Services.DTOs.Common;
using FloristHub.Services.DTOs.Orders;
using FloristHub.Services.DTOs.Legal;
using FloristHub.Services.DTOs.Payments;
using FloristHub.Services;
using FloristHub.Services.Marketing;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly IReviewService _reviews;

    public OrdersController(IOrderService orders, IReviewService reviews)
    {
        _orders = orders;
        _reviews = reviews;
    }

    [Authorize(Roles = Roles.Customer)]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct) =>
        Ok(await _orders.CreateAsync(request, User.GetUserId(), idempotencyKey, ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> MyOrders(CancellationToken ct) =>
        Ok(await _orders.ListForCustomerAsync(User.GetUserId(), ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpGet("manage")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> Manage([FromQuery] OrderStatus? status, CancellationToken ct) =>
        Ok(await _orders.ListForFloristAsync(status, ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpGet("manage/paged")]
    public async Task<ActionResult<PagedResult<OrderDto>>> ManagePaged(
        [FromQuery] OrderStatus? status,
        [FromQuery] DeliveryType? deliveryType,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] string? customerSearch,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await _orders.ListForFloristPagedAsync(status, deliveryType, fromDate, toDate, customerSearch, page, pageSize, ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> Pending(CancellationToken ct) =>
        Ok(await _orders.ListForFloristAsync(OrderStatus.PendingConfirmation, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken ct)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        return Ok(await _orders.GetByIdAsync(id, User.GetUserId(), role, ct));
    }

    [Authorize(Roles = Roles.Florist)]
    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<OrderDto>> Confirm(Guid id, [FromBody] FloristOrderActionRequest request, CancellationToken ct) =>
        Ok(await _orders.ConfirmAsync(id, User.GetUserId(), request, ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<OrderDto>> Reject(Guid id, [FromBody] FloristOrderActionRequest request, CancellationToken ct) =>
        Ok(await _orders.RejectAsync(id, User.GetUserId(), request, ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPost("{id:guid}/proposals")]
    public async Task<ActionResult<OrderDto>> ProposeChanges(Guid id, [FromBody] CreateOrderChangeProposalRequest request, CancellationToken ct) =>
        Ok(await _orders.ProposeChangesAsync(id, User.GetUserId(), request, ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{orderId:guid}/proposals/{proposalId:guid}/accept")]
    public async Task<ActionResult<OrderDto>> AcceptProposal(
        Guid orderId,
        Guid proposalId,
        [FromBody] RespondToOrderChangeProposalRequest request,
        CancellationToken ct) =>
        Ok(await _orders.AcceptChangeProposalAsync(orderId, proposalId, User.GetUserId(), request, ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{orderId:guid}/proposals/{proposalId:guid}/reject")]
    public async Task<ActionResult<OrderDto>> RejectProposal(
        Guid orderId,
        Guid proposalId,
        [FromBody] RespondToOrderChangeProposalRequest request,
        CancellationToken ct) =>
        Ok(await _orders.RejectChangeProposalAsync(orderId, proposalId, User.GetUserId(), request, ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{id:guid}/coupon")]
    public async Task<ActionResult<OrderDto>> ApplyCoupon(Guid id, [FromBody] ApplyCouponRequest request, CancellationToken ct) =>
        Ok(await _orders.ApplyCouponAsync(id, User.GetUserId(), request.Code, ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpDelete("{id:guid}/coupon")]
    public async Task<ActionResult<OrderDto>> RemoveCoupon(Guid id, CancellationToken ct) =>
        Ok(await _orders.RemoveCouponAsync(id, User.GetUserId(), ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{id:guid}/review")]
    public async Task<ActionResult<OrderReviewDto>> Review(Guid id, [FromBody] CreateReviewRequest request, CancellationToken ct) =>
        Ok(await _reviews.CreateAsync(id, User.GetUserId(), request, ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id, [FromBody] CancelOrderRequest request, CancellationToken ct) =>
        Ok(await _orders.CancelAsync(id, User.GetUserId(), request, ct));

    [Authorize(Roles = Roles.Florist)]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct) =>
        Ok(await _orders.UpdateStatusAsync(id, User.GetUserId(), request, ct));

    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{id:guid}/payment")]
    public async Task<ActionResult<CheckoutSessionDto>> InitiatePayment(
        Guid id,
        [FromBody] StartCheckoutRequest? request,
        CancellationToken ct) =>
        Ok(await _orders.InitiatePaymentAsync(
            id,
            User.GetUserId(),
            request?.SuccessUrl,
            request?.CancelUrl,
            request is null
                ? null
                : new LegalConsentRequest(
                    request.AcceptTermsAndPrivacy,
                    request.TermsVersion ?? string.Empty,
                    request.PrivacyVersion ?? string.Empty),
            request?.CustomerTaxId,
            ct));
}
