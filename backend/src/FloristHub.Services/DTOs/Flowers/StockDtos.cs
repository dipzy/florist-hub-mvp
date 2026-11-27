using FloristHub.DAL.Enums;

namespace FloristHub.Services.DTOs.Flowers;

public record StockMovementDto(
    Guid Id,
    Guid FlowerId,
    Guid? OrderId,
    int Delta,
    int? BalanceAfter,
    StockMovementReason Reason,
    string? Note,
    DateTimeOffset CreatedAt);

public record LowStockAlertDto(
    Guid FlowerId,
    string Name,
    int StockQuantity,
    int MinimumStockQuantity);
