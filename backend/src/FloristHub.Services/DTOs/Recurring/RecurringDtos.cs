using FloristHub.DAL.Enums;
using FloristHub.Services.DTOs.Orders;

namespace FloristHub.Services.DTOs.Recurring;

public record CreateRecurringSubscriptionRequest(
    RecurringFrequency Frequency,
    DateTimeOffset AnchorDeliveryAt,
    DeliveryType DeliveryType,
    string? DeliveryAddress,
    Guid? DeliveryZoneId,
    ArrangementBaseType BaseType,
    string? CustomerNotes,
    bool IsFloristChoice,
    decimal? FloristChoiceBudget,
    IReadOnlyList<OrderItemRequest> Items,
    ArrangementShape ArrangementShape = ArrangementShape.Sponge,
    int ArrangementVariationSeed = 0,
    string? ArrangementPreviewUrl = null,
    bool AcceptTermsAndPrivacy = false,
    string? TermsVersion = null,
    string? PrivacyVersion = null);

public record RecurringSubscriptionItemDto(Guid Id, Guid? FlowerId, string Name, int Quantity, decimal UnitPrice);

public record RecurringOccurrenceDto(
    Guid Id,
    DateTimeOffset ScheduledDeliveryAt,
    RecurringOccurrenceStatus Status,
    Guid? GeneratedOrderId);

public record RecurringSubscriptionDto(
    Guid Id,
    RecurringSubscriptionStatus Status,
    RecurringFrequency Frequency,
    DateTimeOffset AnchorDeliveryAt,
    DateTimeOffset? NextDeliveryAt,
    DeliveryType DeliveryType,
    string? DeliveryAddress,
    Guid? DeliveryZoneId,
    string? DeliveryZoneName,
    ArrangementBaseType BaseType,
    bool IsFloristChoice,
    decimal? FloristChoiceBudget,
    decimal EstimatedItemsTotal,
    decimal DeliveryFee,
    string? CustomerNotes,
    string? FloristResponseNotes,
    string? CustomerName,
    DateTimeOffset CreatedAt,
    IReadOnlyList<RecurringSubscriptionItemDto> Items,
    IReadOnlyList<RecurringOccurrenceDto> RecentOccurrences);

public record FloristRecurringActionRequest(string? Notes);

public record FloristUpcomingRecurringDeliveryDto(
    Guid SubscriptionId,
    Guid? CustomerId,
    string? CustomerName,
    DateTimeOffset ScheduledDeliveryAt,
    DeliveryType DeliveryType,
    string? DeliveryAddress,
    bool IsFloristChoice);
