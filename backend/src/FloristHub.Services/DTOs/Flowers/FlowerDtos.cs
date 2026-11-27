namespace FloristHub.Services.DTOs.Flowers;

public record FlowerDto(
    Guid Id,
    string Name,
    string? Color,
    decimal UnitPrice,
    string Unit,
    int? StockQuantity,
    bool IsApproximatelyAvailable,
    bool IsCatalogActive,
    string? ImageUrl,
    string? Notes,
    decimal? VatRate = null,
    int? MinimumStockQuantity = null,
    bool IsOutOfStock = false,
    bool IsLowStock = false,
    string? Category = null,
    IReadOnlyList<int>? SeasonMonths = null);

public record CreateFlowerRequest(
    string Name,
    string? Color,
    decimal UnitPrice,
    string? Unit,
    int? StockQuantity,
    bool IsApproximatelyAvailable,
    string? ImageUrl,
    string? Notes,
    decimal? VatRate = null,
    int? MinimumStockQuantity = null,
    string? Category = null,
    IReadOnlyList<int>? SeasonMonths = null);

public record UpdateFlowerRequest(
    string Name,
    string? Color,
    decimal UnitPrice,
    string? Unit,
    int? StockQuantity,
    bool IsApproximatelyAvailable,
    bool IsCatalogActive,
    string? ImageUrl,
    string? Notes,
    decimal? VatRate = null,
    int? MinimumStockQuantity = null,
    string? Category = null,
    IReadOnlyList<int>? SeasonMonths = null);
