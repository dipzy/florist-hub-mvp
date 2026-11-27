using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Portfolio;
using FloristHub.Services.Marketing;
using FloristHub.Services.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services;

public interface IPortfolioService
{
    Task<IReadOnlyList<PortfolioArrangementDto>> ListActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PortfolioArrangementDto>> ListForFloristAsync(CancellationToken ct = default);
    Task<PortfolioArrangementDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PortfolioArrangementDto> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<PortfolioArrangementDto> CreateAsync(CreatePortfolioArrangementRequest request, Guid userId, CancellationToken ct = default);
    Task<PortfolioArrangementDto> UpdateAsync(Guid id, UpdatePortfolioArrangementRequest request, Guid userId, CancellationToken ct = default);
    Task<PortfolioArrangementDto> SetStatusAsync(Guid id, PortfolioArrangementStatus status, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<PortfolioArrangementDto>> UpdateSortOrdersAsync(UpdatePortfolioSortOrdersRequest request, Guid userId, CancellationToken ct = default);
}

public class PortfolioService : IPortfolioService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;

    public PortfolioService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<PortfolioArrangementDto>> ListActiveAsync(CancellationToken ct = default)
    {
        await TenantPublicAccessRules.RequirePublishedTenantAsync(_tenant, _unitOfWork, ct);
        var items = await _unitOfWork.Portfolio.QueryWithDetails()
            .Where(p => p.Status == PortfolioArrangementStatus.Active)
            .OrderBy(p => p.SortOrder).ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PortfolioArrangementDto>> ListForFloristAsync(CancellationToken ct = default)
    {
        var items = await _unitOfWork.Portfolio.QueryWithDetails()
            .OrderBy(p => p.SortOrder).ThenByDescending(p => p.CreatedAt).ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task<PortfolioArrangementDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        RequireTenant();
        var entity = await _unitOfWork.Portfolio.QueryWithDetails().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException("Arranjo do portfolio não encontrado.");
        if (_tenant.TenantId is Guid tenantId && entity.TenantId != tenantId)
            throw new KeyNotFoundException("Arranjo do portfolio não encontrado.");
        return Map(entity);
    }

    public async Task<PortfolioArrangementDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        RequireTenant();
        var normalized = (slug ?? "").Trim().ToLowerInvariant();
        var entity = await _unitOfWork.Portfolio.QueryWithDetails()
            .FirstOrDefaultAsync(p => p.Slug == normalized && p.Status == PortfolioArrangementStatus.Active, ct)
            ?? throw new KeyNotFoundException("Arranjo do portfolio não encontrado.");
        return Map(entity);
    }

    public async Task<PortfolioArrangementDto> CreateAsync(CreatePortfolioArrangementRequest request, Guid userId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var slug = await UniqueSlugAsync(PublicSlug.From(request.Title), null, ct);
        var arrangement = new PortfolioArrangement
        {
            TenantId = tenantId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Price = request.Price,
            Tags = request.Tags,
            Status = PortfolioArrangementStatus.Active,
            Slug = slug
        };
        ApplyChildren(arrangement, request.Photos, request.Items);
        await _unitOfWork.Portfolio.AddAsync(arrangement, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("PortfolioArrangementCreated", userId, tenantId, nameof(PortfolioArrangement), arrangement.Id, arrangement.Title, ct);
        return Map(arrangement);
    }

    public async Task<PortfolioArrangementDto> UpdateAsync(Guid id, UpdatePortfolioArrangementRequest request, Guid userId, CancellationToken ct = default)
    {
        var arrangement = await _unitOfWork.Portfolio.GetByIdWithDetailsForUpdateAsync(id, ct)
            ?? throw new KeyNotFoundException("Arranjo do portfolio não encontrado.");

        arrangement.Title = request.Title.Trim();
        arrangement.Slug = await UniqueSlugAsync(PublicSlug.From(request.Title), arrangement.Id, ct);
        arrangement.Description = request.Description;
        arrangement.Price = request.Price;
        arrangement.Tags = request.Tags;
        arrangement.Status = request.Status;
        arrangement.UpdatedAt = DateTimeOffset.UtcNow;

        arrangement.Photos.Clear();
        arrangement.Items.Clear();
        ApplyChildren(arrangement, request.Photos, request.Items);

        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("PortfolioArrangementUpdated", userId, arrangement.TenantId, nameof(PortfolioArrangement), arrangement.Id, arrangement.Title, ct);
        return Map(arrangement);
    }

    public async Task<PortfolioArrangementDto> SetStatusAsync(Guid id, PortfolioArrangementStatus status, Guid userId, CancellationToken ct = default)
    {
        var arrangement = await _unitOfWork.Portfolio.GetByIdWithDetailsForUpdateAsync(id, ct)
            ?? throw new KeyNotFoundException("Arranjo do portfolio não encontrado.");
        arrangement.Status = status;
        arrangement.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("PortfolioArrangementStatusChanged", userId, arrangement.TenantId, nameof(PortfolioArrangement), arrangement.Id, status.ToString(), ct);
        return Map(arrangement);
    }

    public async Task<IReadOnlyList<PortfolioArrangementDto>> UpdateSortOrdersAsync(UpdatePortfolioSortOrdersRequest request, Guid userId, CancellationToken ct = default)
    {
        if (request.Items.Count == 0)
            return await ListForFloristAsync(ct);

        var ids = request.Items.Select(i => i.Id).ToList();
        var arrangements = await _unitOfWork.Portfolio.Query().Where(p => ids.Contains(p.Id)).ToListAsync(ct);
        foreach (var input in request.Items)
        {
            var entity = arrangements.FirstOrDefault(p => p.Id == input.Id);
            if (entity is null) continue;
            entity.SortOrder = input.SortOrder;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("PortfolioArrangementReordered", userId, RequireTenant(), nameof(PortfolioArrangement), Guid.Empty, $"{request.Items.Count} items", ct);
        return await ListForFloristAsync(ct);
    }

    private static void ApplyChildren(
        PortfolioArrangement arrangement,
        IReadOnlyList<PortfolioPhotoDto> photos,
        IReadOnlyList<PortfolioItemDto> items)
    {
        foreach (var photo in photos)
            arrangement.Photos.Add(new PortfolioArrangementPhoto
            {
                TenantId = arrangement.TenantId,
                Url = photo.Url,
                SortOrder = photo.SortOrder
            });

        foreach (var item in items)
            arrangement.Items.Add(new PortfolioArrangementItem
            {
                TenantId = arrangement.TenantId,
                Name = item.Name,
                Quantity = item.Quantity,
                IsMaterial = item.IsMaterial,
                FlowerId = item.FlowerId
            });
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");

    private static PortfolioArrangementDto Map(PortfolioArrangement p) =>
        new(
            p.Id,
            p.Title,
            p.Description,
            p.Price,
            p.Tags,
            p.Status,
            p.SortOrder,
            p.Photos.Select(x => new PortfolioPhotoDto(x.Url, x.SortOrder)).ToList(),
            p.Items.Select(x => new PortfolioItemDto(x.Name, x.Quantity, x.IsMaterial, x.FlowerId)).ToList(),
            p.Slug);

    private async Task<string> UniqueSlugAsync(string seed, Guid? exceptId, CancellationToken ct)
    {
        var slug = seed;
        var suffix = 2;
        while (await _unitOfWork.Portfolio.Query().AnyAsync(p => p.Slug == slug && p.Id != exceptId, ct))
        {
            var tail = "-" + suffix;
            var stem = seed.Length + tail.Length > 80 ? seed[..(80 - tail.Length)] : seed;
            slug = stem + tail;
            suffix++;
        }

        return slug;
    }
}
