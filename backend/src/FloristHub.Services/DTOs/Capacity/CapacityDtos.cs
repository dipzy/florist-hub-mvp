namespace FloristHub.Services.DTOs.Capacity;

public record DeliveryWindowDto(string Label, string Start, string End, int OrderLimit);

public record DeliveryWindowInput(string Label, string Start, string End, int OrderLimit);

public record DeliveryExceptionDto(DateOnly Date, bool IsClosed, int? DailyOrderLimitOverride, string? Note);

public record DeliveryCapacitySettingsDto(
    int? DailyOrderLimit,
    IReadOnlyList<DeliveryWindowDto> Windows,
    IReadOnlyList<DeliveryExceptionDto> Exceptions);

public record UpdateDeliveryCapacitySettingsRequest(int DailyOrderLimit, IReadOnlyList<DeliveryWindowInput> Windows);

public record UpsertDeliveryCapacityExceptionRequest(DateOnly Date, bool IsClosed, int? DailyOrderLimitOverride, string? Note);

public record PublicCapacityWindowDto(string Label, string Start, string End, int Limit, int Booked, int Remaining);

public record PublicCapacityDayDto(
    DateOnly Date,
    bool Closed,
    int? DailyLimit,
    int Booked,
    int? Remaining,
    IReadOnlyList<PublicCapacityWindowDto> Windows);

public record FloristCapacityDayDto(
    DateOnly Date,
    bool Closed,
    int? DailyLimit,
    int Booked,
    int? Remaining,
    string? Note,
    IReadOnlyList<PublicCapacityWindowDto> Windows);
