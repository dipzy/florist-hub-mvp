using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Marketing;

public interface ICampaignService
{
    Task<IReadOnlyList<CampaignDto>> ListAsync(CancellationToken ct = default);
    Task<CampaignDto> CreateAsync(SaveCampaignRequest request, Guid userId, CancellationToken ct = default);
    Task<CampaignDto> UpdateAsync(Guid id, SaveCampaignRequest request, Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}

public sealed class CampaignService : ICampaignService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;

    public CampaignService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CampaignDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = await Query().OrderByDescending(c => c.StartsOn).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<CampaignDto> CreateAsync(SaveCampaignRequest request, Guid userId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var campaign = new SeasonalCampaign { TenantId = tenantId, Name = request.Name.Trim(), Slug = "campanha" };
        await ApplyAsync(campaign, request, ct);
        campaign.Slug = await UniqueSlugAsync(PublicSlug.From(campaign.Name), null, ct);
        await _unitOfWork.Campaigns.AddAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("CampaignCreated", userId, tenantId, nameof(SeasonalCampaign), campaign.Id, campaign.Name, ct);
        return Map(await ReloadAsync(campaign.Id, ct));
    }

    public async Task<CampaignDto> UpdateAsync(Guid id, SaveCampaignRequest request, Guid userId, CancellationToken ct = default)
    {
        var campaign = await Query().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException("Campanha não encontrada.");
        await ApplyAsync(campaign, request, ct);
        campaign.Slug = await UniqueSlugAsync(PublicSlug.From(campaign.Name), campaign.Id, ct);
        campaign.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("CampaignUpdated", userId, campaign.TenantId, nameof(SeasonalCampaign), campaign.Id, campaign.Name, ct);
        return Map(await ReloadAsync(campaign.Id, ct));
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var campaign = await _unitOfWork.Campaigns.Query().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException("Campanha não encontrada.");
        _unitOfWork.Campaigns.Remove(campaign);
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("CampaignDeleted", userId, campaign.TenantId, nameof(SeasonalCampaign), campaign.Id, campaign.Name, ct);
    }

    private async Task ApplyAsync(SeasonalCampaign campaign, SaveCampaignRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120)
            throw new InvalidOperationException("Indique o nome da campanha (até 120 caracteres).");
        if (request.EndsOn < request.StartsOn)
            throw new InvalidOperationException("A campanha tem de terminar no próprio dia ou depois.");
        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 2000)
            throw new InvalidOperationException("A descrição da campanha é demasiado longa.");

        campaign.Name = request.Name.Trim();
        campaign.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        campaign.StartsOn = request.StartsOn;
        campaign.EndsOn = request.EndsOn;
        campaign.HighlightOnHome = request.HighlightOnHome;
        campaign.IsActive = request.IsActive;
        campaign.Flowers.Clear();

        var inputs = request.Flowers ?? [];
        var ids = inputs.Select(f => f.FlowerId).Distinct().ToList();
        if (ids.Count != inputs.Count)
            throw new InvalidOperationException("Cada flor só pode entrar uma vez na campanha.");
        if (ids.Count > 0)
        {
            var known = await _unitOfWork.Flowers.Query().CountAsync(f => ids.Contains(f.Id), ct);
            if (known != ids.Count)
                throw new InvalidOperationException("Uma das flores da campanha não existe nesta loja.");
        }

        foreach (var input in inputs)
        {
            if (input.SpecialPrice is decimal price)
            {
                CouponMath.EnsureMoney(price, "O preço especial");
                if (price <= 0)
                    throw new InvalidOperationException("O preço especial tem de ser superior a zero.");
            }

            campaign.Flowers.Add(new CampaignFlower
            {
                TenantId = campaign.TenantId,
                FlowerId = input.FlowerId,
                SpecialPrice = input.SpecialPrice,
                SortOrder = input.SortOrder
            });
        }
    }

    private IQueryable<SeasonalCampaign> Query() =>
        _unitOfWork.Campaigns.Query().Include(c => c.Flowers).ThenInclude(f => f.Flower);

    private async Task<SeasonalCampaign> ReloadAsync(Guid id, CancellationToken ct) =>
        await Query().FirstAsync(c => c.Id == id, ct);

    private async Task<string> UniqueSlugAsync(string seed, Guid? exceptId, CancellationToken ct)
    {
        var slug = seed;
        var suffix = 2;
        while (await _unitOfWork.Campaigns.Query().AnyAsync(c => c.Slug == slug && c.Id != exceptId, ct))
        {
            var tail = "-" + suffix;
            var stem = seed.Length + tail.Length > 80 ? seed[..(80 - tail.Length)] : seed;
            slug = stem + tail;
            suffix++;
        }

        return slug;
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");

    private static CampaignDto Map(SeasonalCampaign campaign) =>
        new(
            campaign.Id,
            campaign.Name,
            campaign.Slug,
            campaign.Description,
            campaign.StartsOn,
            campaign.EndsOn,
            campaign.HighlightOnHome,
            campaign.IsActive,
            campaign.Flowers
                .OrderBy(f => f.SortOrder)
                .Select(f => new CampaignFlowerDto(f.FlowerId, f.Flower?.Name ?? "Flor", f.Flower?.UnitPrice ?? 0, f.SpecialPrice, f.SortOrder, f.Flower?.StockQuantity is <= 0))
                .ToList());
}
