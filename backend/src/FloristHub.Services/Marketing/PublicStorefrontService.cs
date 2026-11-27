using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Marketing;

public interface IPublicStorefrontService
{
    Task<PublicStorefrontDto> GetAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PublicSeasonalFlowerDto>> SeasonalAsync(int? month, CancellationToken ct = default);
    Task<PublicCampaignDto?> CampaignBySlugAsync(string slug, CancellationToken ct = default);
}

public sealed class PublicStorefrontService : IPublicStorefrontService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IReviewService _reviews;

    public PublicStorefrontService(IUnitOfWork unitOfWork, ITenantContext tenant, IReviewService reviews)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _reviews = reviews;
    }

    public async Task<PublicStorefrontDto> GetAsync(CancellationToken ct = default)
    {
        var tenant = await RequireTenantAsync(ct);
        var today = LisbonClock.Today();
        var campaign = await ActiveCampaigns(today)
            .Where(c => c.HighlightOnHome)
            .OrderByDescending(c => c.StartsOn)
            .FirstOrDefaultAsync(ct);
        var summary = await _reviews.SummaryAsync(ct);
        var seasonal = await SeasonalAsync(today.Month, ct);
        var heroImage = await ResolveHeroImageAsync(tenant, ct);
        return new PublicStorefrontDto(
            tenant.Name,
            tenant.Description,
            heroImage,
            campaign is null ? null : MapCampaign(campaign),
            seasonal,
            summary.AverageRating,
            summary.ReviewCount);
    }

    private async Task<string?> ResolveHeroImageAsync(Tenant tenant, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(tenant.LogoUrl))
            return tenant.LogoUrl.Trim();
        var firstPhoto = await _unitOfWork.Portfolio.Query()
            .AsNoTracking()
            .Where(p => p.Status == PortfolioArrangementStatus.Active)
            .OrderByDescending(p => p.UpdatedAt)
            .SelectMany(p => p.Photos.OrderBy(ph => ph.SortOrder).Take(1))
            .Select(ph => ph.Url)
            .FirstOrDefaultAsync(ct);
        return string.IsNullOrWhiteSpace(firstPhoto) ? null : firstPhoto;
    }

    public async Task<IReadOnlyList<PublicSeasonalFlowerDto>> SeasonalAsync(int? month, CancellationToken ct = default)
    {
        await RequireTenantAsync(ct);
        var target = month ?? LisbonClock.Today().Month;
        if (target is < 1 or > 12)
            throw new InvalidOperationException("Mês inválido.");
        var today = LisbonClock.Today();
        var flowers = await _unitOfWork.Flowers.Query()
            .Where(f => f.IsCatalogActive && f.SeasonMonths != 0)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
        var prices = await SpecialPricesAsync(today, ct);
        return flowers
            .Where(f => SeasonMonths.Includes(f.SeasonMonths, target))
            .Select(f => new PublicSeasonalFlowerDto(
                f.Id,
                f.Name,
                f.Color,
                f.Category,
                f.UnitPrice,
                prices.GetValueOrDefault(f.Id),
                f.ImageUrl,
                SeasonMonths.FromMask(f.SeasonMonths),
                f.StockQuantity is <= 0))
            .ToList();
    }

    public async Task<PublicCampaignDto?> CampaignBySlugAsync(string slug, CancellationToken ct = default)
    {
        await RequireTenantAsync(ct);
        var today = LisbonClock.Today();
        var campaign = await ActiveCampaigns(today).FirstOrDefaultAsync(c => c.Slug == slug, ct);
        return campaign is null ? null : MapCampaign(campaign);
    }

    private IQueryable<SeasonalCampaign> ActiveCampaigns(DateOnly today) =>
        _unitOfWork.Campaigns.Query()
            .Include(c => c.Flowers).ThenInclude(f => f.Flower)
            .Where(c => c.IsActive && c.StartsOn <= today && c.EndsOn >= today);

    private async Task<Dictionary<Guid, decimal?>> SpecialPricesAsync(DateOnly today, CancellationToken ct)
    {
        var rows = await _unitOfWork.CampaignFlowers.Query()
            .Where(f => f.SpecialPrice != null && f.Campaign!.IsActive && f.Campaign.StartsOn <= today && f.Campaign.EndsOn >= today)
            .Select(f => new { f.FlowerId, Price = f.SpecialPrice })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.FlowerId).ToDictionary(g => g.Key, g => g.Min(x => x.Price));
    }

    private Task<Tenant> RequireTenantAsync(CancellationToken ct) =>
        TenantPublicAccessRules.RequirePublishedTenantAsync(_tenant, _unitOfWork, ct);

    private static PublicCampaignDto MapCampaign(SeasonalCampaign campaign) =>
        new(
            campaign.Name,
            campaign.Slug,
            campaign.Description,
            campaign.StartsOn,
            campaign.EndsOn,
            campaign.Flowers
                .Where(f => f.Flower is { IsCatalogActive: true })
                .OrderBy(f => f.SortOrder)
                .Select(f => new CampaignFlowerDto(
                    f.FlowerId,
                    f.Flower!.Name,
                    f.Flower.UnitPrice,
                    f.SpecialPrice,
                    f.SortOrder,
                    f.Flower.StockQuantity is <= 0))
                .ToList());
}
