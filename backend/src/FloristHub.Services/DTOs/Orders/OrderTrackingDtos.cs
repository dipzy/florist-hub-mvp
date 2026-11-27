using FloristHub.DAL.Enums;

namespace FloristHub.Services.DTOs.Orders;

public record PublicOrderStatusStepDto(OrderStatus Status, DateTimeOffset At);

public record PublicOrderTrackingDto(
    Guid OrderId,
    OrderStatus Status,
    DeliveryType DeliveryType,
    DateTimeOffset? RequestedDeliveryAt,
    DateOnly? CapacityServiceDate,
    string? DeliveryWindowLabel,
    string? DeliveryWindowStart,
    string? DeliveryWindowEnd,
    IReadOnlyList<PublicOrderStatusStepDto> Timeline);
