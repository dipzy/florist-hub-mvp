using FloristHub.DAL.Enums;
using FloristHub.Services.DTOs.Payments;

namespace FloristHub.Services.DTOs.Orders;

public record OrderItemRequest(Guid? FlowerId, string Name, int Quantity, decimal UnitPrice);

public record CreateOrderRequest(
    ArrangementBaseType BaseType,
    DeliveryType DeliveryType,
    string? DeliveryAddress,
    DateTimeOffset? RequestedDeliveryAt,
    string? CustomerNotes,
    Guid? InspiredByPortfolioArrangementId,
    Guid? DeliveryZoneId,
    IReadOnlyList<OrderItemRequest> Items,
    ArrangementShape ArrangementShape = ArrangementShape.Sponge,
    int ArrangementVariationSeed = 0,
    string? ArrangementPreviewUrl = null,
    bool AcceptTermsAndPrivacy = false,
    string? TermsVersion = null,
    string? PrivacyVersion = null,
    string? CustomerTaxId = null,
    string? CouponCode = null);

public record OrderItemDto(Guid Id, Guid? FlowerId, string Name, int Quantity, decimal UnitPrice, decimal VatRate = 23m);

public record OrderStatusHistoryDto(Guid Id, OrderStatus FromStatus, OrderStatus ToStatus, DateTimeOffset CreatedAt, string? Notes);

public record OrderChangeProposalDto(
    Guid Id,
    OrderChangeProposalStatus Status,
    string Message,
    string? ProposedItemChangesJson,
    decimal? RevisedTotal,
    DateTimeOffset CreatedAt);

public record OrderDto(
    Guid Id,
    OrderStatus Status,
    ArrangementBaseType BaseType,
    DeliveryType DeliveryType,
    string? DeliveryAddress,
    DateTimeOffset? RequestedDeliveryAt,
    string? CustomerNotes,
    Guid? InspiredByPortfolioArrangementId,
    decimal EstimatedTotal,
    decimal DeliveryFee,
    Guid? DeliveryZoneId,
    string? DeliveryZoneName,
    bool PaymentCaptured,
    string? Arrangement3DPreviewToken,
    string? CustomerName,
    string? CustomerEmail,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistory,
    IReadOnlyList<OrderChangeProposalDto> ChangeProposals,
    ArrangementShape ArrangementShape,
    int ArrangementVariationSeed,
    string? ArrangementPreviewUrl,
    OrderPaymentDto? Payment = null,
    string? CustomerTaxId = null,
    decimal DiscountAmount = 0,
    string? CouponCode = null,
    OrderReviewSummaryDto? Review = null,
    string? TrackingAccessToken = null);

public record OrderReviewSummaryDto(Guid Id, int Stars, string? Comment, string? FloristReply, bool IsHidden);

public record FloristOrderActionRequest(string? Notes);

public record CreateOrderChangeProposalRequest(string Message, string? ProposedItemChangesJson, decimal? RevisedTotal);

public record UpdateOrderStatusRequest(OrderStatus NewStatus, string? Notes);

public record RespondToOrderChangeProposalRequest(string? Notes);

public record CancelOrderRequest(string? Notes);
