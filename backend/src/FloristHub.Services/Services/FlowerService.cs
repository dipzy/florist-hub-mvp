using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Common;
using FloristHub.Services.DTOs.Flowers;
using FloristHub.Services.Marketing;
using FloristHub.Services.Invoicing;
using FloristHub.Services.Localization;
using FloristHub.Services.Onboarding;
using FloristHub.Services.Stock;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services;

public interface IFlowerService
{
    Task<IReadOnlyList<FlowerDto>> ListAsync(CancellationToken ct = default);
    Task<PagedResult<FlowerDto>> ListManageAsync(string? search, bool? catalogActive, int page, int pageSize, CancellationToken ct = default);
    Task<FlowerDto> CreateAsync(CreateFlowerRequest request, Guid userId, CancellationToken ct = default);
    Task<FlowerDto> UpdateAsync(Guid id, UpdateFlowerRequest request, Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}

public class FlowerService : IFlowerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;
    private readonly IStockService? _stock;

    public FlowerService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditLogger audit, IStockService? stock = null)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
        _stock = stock;
    }

    public async Task<IReadOnlyList<FlowerDto>> ListAsync(CancellationToken ct = default)
    {
        await TenantPublicAccessRules.RequirePublishedTenantAsync(_tenant, _unitOfWork, ct);
        var flowers = (await _unitOfWork.Flowers.ListOrderedByNameAsync(ct))
            .Where(f => f.IsCatalogActive)
            .ToList();
        var today = LisbonClock.Today();
        var offers = await _unitOfWork.CampaignFlowers.Query()
            .Where(f => f.SpecialPrice != null && f.Campaign!.IsActive && f.Campaign.StartsOn <= today && f.Campaign.EndsOn >= today)
            .Select(f => new { f.FlowerId, Price = f.SpecialPrice!.Value })
            .ToListAsync(ct);
        var best = offers.GroupBy(o => o.FlowerId).ToDictionary(g => g.Key, g => g.Min(x => x.Price));
        return flowers.Select(flower =>
        {
            var dto = Map(flower);
            if (flower.StockQuantity is not <= 0 && best.TryGetValue(flower.Id, out var price))
                dto = dto with { UnitPrice = price };
            if (FlowerPublicCatalog.IsForSale(flower.IsCatalogActive, flower.StockQuantity, dto.IsOutOfStock))
                dto = dto with { IsApproximatelyAvailable = true };
            return dto;
        }).ToList();
    }

    public async Task<PagedResult<FlowerDto>> ListManageAsync(
        string? search,
        bool? catalogActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (items, total) = await _unitOfWork.Flowers.SearchPagedAsync(search, catalogActive, page, pageSize, ct);
        return new PagedResult<FlowerDto>(items.Select(Map).ToList(), total, Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
    }

    public async Task<FlowerDto> CreateAsync(CreateFlowerRequest request, Guid userId, CancellationToken ct = default)
    {
        FlowerValidation.EnsureValid(request);
        var tenantId = RequireTenant();
        var flower = new Flower
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Color = request.Color,
            UnitPrice = request.UnitPrice,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "unidade" : request.Unit!.Trim(),
            StockQuantity = request.StockQuantity,
            IsApproximatelyAvailable = request.IsApproximatelyAvailable,
            IsCatalogActive = true,
            ImageUrl = request.ImageUrl,
            Notes = request.Notes,
            VatRate = NormalizeVat(request.VatRate),
            MinimumStockQuantity = request.MinimumStockQuantity,
            Category = CleanCategory(request.Category),
            SeasonMonths = SeasonMonths.ToMask(request.SeasonMonths)
        };
        await _unitOfWork.Flowers.AddAsync(flower, ct);
        _stock?.RecordAdjustment(flower, null, null);
        await _unitOfWork.SaveChangesAsync(ct);
        if (_stock is not null)
            await _stock.FlushNoticesAsync(ct);
        await _audit.LogAsync("FlowerCreated", userId, tenantId, nameof(Flower), flower.Id, flower.Name, ct);
        return Map(flower);
    }

    public async Task<FlowerDto> UpdateAsync(Guid id, UpdateFlowerRequest request, Guid userId, CancellationToken ct = default)
    {
        FlowerValidation.EnsureValidUpdate(request);
        var flower = await _unitOfWork.Flowers.Query().FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new KeyNotFoundException("Flor não encontrada.");

        flower.Name = request.Name.Trim();
        flower.Color = request.Color;
        flower.UnitPrice = request.UnitPrice;
        flower.Unit = string.IsNullOrWhiteSpace(request.Unit) ? "unidade" : request.Unit.Trim();
        var previousQuantity = flower.StockQuantity;
        var previousMinimum = flower.MinimumStockQuantity;
        flower.StockQuantity = request.StockQuantity;
        flower.MinimumStockQuantity = request.MinimumStockQuantity;
        flower.IsApproximatelyAvailable = request.IsApproximatelyAvailable;
        flower.IsCatalogActive = request.IsCatalogActive;
        flower.ImageUrl = request.ImageUrl;
        flower.Notes = request.Notes;
        flower.VatRate = NormalizeVat(request.VatRate);
        flower.Category = CleanCategory(request.Category);
        flower.SeasonMonths = SeasonMonths.ToMask(request.SeasonMonths);
        flower.UpdatedAt = DateTimeOffset.UtcNow;
        _stock?.RecordAdjustment(flower, previousQuantity, previousMinimum);
        await _unitOfWork.SaveChangesAsync(ct);
        if (_stock is not null)
            await _stock.FlushNoticesAsync(ct);
        await _audit.LogAsync("FlowerUpdated", userId, flower.TenantId, nameof(Flower), flower.Id, flower.Name, ct);
        return Map(flower);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var flower = await _unitOfWork.Flowers.Query().FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new KeyNotFoundException("Flor não encontrada.");

        if (await _unitOfWork.Flowers.IsReferencedInOrdersAsync(id, ct))
        {
            flower.IsCatalogActive = false;
            flower.UpdatedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            await _audit.LogAsync("FlowerDeactivated", userId, flower.TenantId, nameof(Flower), flower.Id, flower.Name, ct);
            return;
        }

        _unitOfWork.Flowers.Remove(flower);
        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("FlowerDeleted", userId, flower.TenantId, nameof(Flower), flower.Id, flower.Name, ct);
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");

    private static FlowerDto Map(Flower f)
    {
        var outOfStock = f.StockQuantity is <= 0;
        var low = f.StockQuantity is int qty && f.MinimumStockQuantity is int min && qty < min;
        return new(
            f.Id,
            f.Name,
            f.Color,
            f.UnitPrice,
            f.Unit,
            f.StockQuantity,
            f.IsApproximatelyAvailable,
            f.IsCatalogActive,
            f.ImageUrl,
            f.Notes,
            f.VatRate,
            f.MinimumStockQuantity,
            outOfStock,
            low,
            f.Category,
            SeasonMonths.FromMask(f.SeasonMonths));
    }

    private static decimal? NormalizeVat(decimal? rate)
    {
        if (rate is null)
            return null;
        if (!VatRates.IsAllowed(rate.Value))
            throw new InvalidOperationException("Taxa de IVA inválida. Use 23, 22, 16, 13, 6 ou 0.");
        return rate;
    }

    private static string? CleanCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? null : CouponMath.NormalizeCategory(category);
}

public static class FlowerValidation
{
    private static readonly CreateFlowerRequestValidator CreateValidator = new();
    private static readonly UpdateFlowerRequestValidator UpdateValidator = new();

    public static void EnsureValid(CreateFlowerRequest request)
    {
        var result = CreateValidator.Validate(request);
        if (!result.IsValid)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }

    public static void EnsureValidUpdate(UpdateFlowerRequest request)
    {
        var result = UpdateValidator.Validate(request);
        if (!result.IsValid)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }
}

public sealed class CreateFlowerRequestValidator : AbstractValidator<CreateFlowerRequest>
{
    public CreateFlowerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Unit).MaximumLength(40);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).When(x => x.StockQuantity.HasValue);
        RuleFor(x => x.MinimumStockQuantity).GreaterThanOrEqualTo(0).When(x => x.MinimumStockQuantity.HasValue);
        RuleFor(x => x.VatRate).Must(rate => rate is null || VatRates.IsAllowed(rate.Value)).WithMessage(_ => ValidationMessages.InvalidVatRate);
    }
}

public sealed class UpdateFlowerRequestValidator : AbstractValidator<UpdateFlowerRequest>
{
    public UpdateFlowerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Unit).MaximumLength(40);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).When(x => x.StockQuantity.HasValue);
        RuleFor(x => x.MinimumStockQuantity).GreaterThanOrEqualTo(0).When(x => x.MinimumStockQuantity.HasValue);
        RuleFor(x => x.VatRate).Must(rate => rate is null || VatRates.IsAllowed(rate.Value)).WithMessage(_ => ValidationMessages.InvalidVatRate);
    }
}
