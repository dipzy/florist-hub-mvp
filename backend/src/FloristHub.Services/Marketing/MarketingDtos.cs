using FloristHub.DAL.Enums;

namespace FloristHub.Services.Marketing;

public record CouponTargetInput(Guid? FlowerId, string? Category);

public record CouponDto(
    Guid Id,
    string Code,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    int? MaxRedemptions,
    int? MaxRedemptionsPerCustomer,
    decimal? MinimumOrderAmount,
    bool IsActive,
    int RedemptionCount,
    IReadOnlyList<CouponTargetInput> Targets);

public record SaveCouponRequest(
    string Code,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    int? MaxRedemptions,
    int? MaxRedemptionsPerCustomer,
    decimal? MinimumOrderAmount,
    bool IsActive,
    IReadOnlyList<CouponTargetInput>? Targets);

public record ApplyCouponRequest(string Code);

public record CampaignFlowerInput(Guid FlowerId, decimal? SpecialPrice, int SortOrder);

public record CampaignFlowerDto(Guid FlowerId, string Name, decimal CatalogPrice, decimal? SpecialPrice, int SortOrder, bool IsOutOfStock = false);

public record CampaignDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    DateOnly StartsOn,
    DateOnly EndsOn,
    bool HighlightOnHome,
    bool IsActive,
    IReadOnlyList<CampaignFlowerDto> Flowers);

public record SaveCampaignRequest(
    string Name,
    string? Description,
    DateOnly StartsOn,
    DateOnly EndsOn,
    bool HighlightOnHome,
    bool IsActive,
    IReadOnlyList<CampaignFlowerInput>? Flowers);

public record PublicCampaignDto(
    string Name,
    string Slug,
    string? Description,
    DateOnly StartsOn,
    DateOnly EndsOn,
    IReadOnlyList<CampaignFlowerDto> Flowers);

public record PublicSeasonalFlowerDto(
    Guid Id,
    string Name,
    string? Color,
    string? Category,
    decimal UnitPrice,
    decimal? SpecialPrice,
    string? ImageUrl,
    int[] SeasonMonths,
    bool IsOutOfStock = false);

public record PublicStorefrontDto(
    string Name,
    string? Description,
    string? HeroImageUrl,
    PublicCampaignDto? Campaign,
    IReadOnlyList<PublicSeasonalFlowerDto> SeasonalFlowers,
    decimal AverageRating,
    int ReviewCount);

public record OrderReviewDto(
    Guid Id,
    Guid OrderId,
    int Stars,
    string? Comment,
    string? FloristReply,
    DateTimeOffset? FloristRepliedAt,
    bool IsHidden,
    bool IsApproved,
    string? CustomerName,
    DateTimeOffset CreatedAt);

public record CreateReviewRequest(int Stars, string? Comment);

public record ReplyReviewRequest(string Reply);

public record HideReviewRequest(bool Hidden, string? Reason);

public record PublicReviewDto(int Stars, string? Comment, string? FloristReply, DateTimeOffset CreatedAt);

public record RatingSummaryDto(decimal AverageRating, int ReviewCount);
