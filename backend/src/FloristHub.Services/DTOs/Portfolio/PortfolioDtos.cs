using FloristHub.DAL.Enums;

namespace FloristHub.Services.DTOs.Portfolio;

public record PortfolioItemDto(string Name, int Quantity, bool IsMaterial, Guid? FlowerId);
public record PortfolioPhotoDto(string Url, int SortOrder);

public record PortfolioArrangementDto(
    Guid Id,
    string Title,
    string? Description,
    decimal? Price,
    string? Tags,
    PortfolioArrangementStatus Status,
    int SortOrder,
    IReadOnlyList<PortfolioPhotoDto> Photos,
    IReadOnlyList<PortfolioItemDto> Items,
    string? Slug = null);

public record CreatePortfolioArrangementRequest(
    string Title,
    string? Description,
    decimal? Price,
    string? Tags,
    IReadOnlyList<PortfolioPhotoDto> Photos,
    IReadOnlyList<PortfolioItemDto> Items);

public record UpdatePortfolioArrangementRequest(
    string Title,
    string? Description,
    decimal? Price,
    string? Tags,
    PortfolioArrangementStatus Status,
    IReadOnlyList<PortfolioPhotoDto> Photos,
    IReadOnlyList<PortfolioItemDto> Items);

public record PortfolioSortOrderItemDto(Guid Id, int SortOrder);

public record UpdatePortfolioSortOrdersRequest(IReadOnlyList<PortfolioSortOrderItemDto> Items);
