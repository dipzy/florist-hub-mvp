using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Store;
using FloristHub.Services.Tenancy;
using FluentValidation;

namespace FloristHub.Services;

public interface IFloristStoreSettingsService
{
    Task<StoreSettingsDto> GetAsync(CancellationToken ct = default);
    Task<StoreSettingsDto> UpdateAsync(UpdateStoreSettingsRequest request, Guid userId, CancellationToken ct = default);
}

public class FloristStoreSettingsService : IFloristStoreSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;

    public FloristStoreSettingsService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<StoreSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var tenant = await LoadTenantAsync(ct);
        return Map(tenant);
    }

    public async Task<StoreSettingsDto> UpdateAsync(UpdateStoreSettingsRequest request, Guid userId, CancellationToken ct = default)
    {
        StoreSettingsValidation.EnsureValid(request);
        var tenantId = RequireTenant();
        var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsForUpdateAsync(tenantId, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");

        tenant.Name = request.Name.Trim();
        tenant.Description = request.Description?.Trim();
        tenant.SeoTitle = string.IsNullOrWhiteSpace(request.SeoTitle) ? null : request.SeoTitle.Trim();
        tenant.SeoDescription = string.IsNullOrWhiteSpace(request.SeoDescription) ? null : request.SeoDescription.Trim();
        tenant.LogoUrl = request.LogoUrl?.Trim();
        tenant.ContactEmail = request.ContactEmail?.Trim();
        tenant.ContactPhone = request.ContactPhone?.Trim();
        tenant.AddressLine = request.AddressLine?.Trim();
        tenant.City = request.City?.Trim();
        tenant.PostalCode = request.PostalCode?.Trim();
        StoreThemeRules.EnsureValidTheme(request.ThemePrimaryColor, request.ThemeAccentColor, request.ThemeFontFamily);
        tenant.ThemePrimaryColor = request.ThemePrimaryColor;
        tenant.ThemeAccentColor = request.ThemeAccentColor;
        tenant.ThemeFontFamily = string.IsNullOrWhiteSpace(request.ThemeFontFamily)
            ? null
            : request.ThemeFontFamily.Trim().ToLowerInvariant();
        tenant.OrdersAcceptingEnabled = request.OrdersAcceptingEnabled;
        tenant.MinimumOrderAdvanceMinutes = Math.Clamp(request.MinimumOrderAdvanceMinutes, 0, 60 * 24 * 14);
        tenant.PickupEnabled = request.PickupEnabled;
        tenant.HomeDeliveryEnabled = request.HomeDeliveryEnabled;
        tenant.CemeteryDeliveryEnabled = request.CemeteryDeliveryEnabled;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        SyncOpeningHours(tenant, request.OpeningHours);
        await SyncDeliveryZonesAsync(tenant, request.DeliveryZones, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("StoreSettingsUpdated", userId, tenantId, nameof(Tenant), tenant.Id, tenant.Name, ct);
        return Map(tenant);
    }

    private async Task<Tenant> LoadTenantAsync(CancellationToken ct)
    {
        var tenantId = RequireTenant();
        return await _unitOfWork.Tenants.GetByIdWithSettingsForUpdateAsync(tenantId, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");
    }

    private static void SyncOpeningHours(Tenant tenant, IReadOnlyList<OpeningHourDto> hours)
    {
        foreach (var input in hours)
        {
            if (input.DayOfWeek is < 0 or > 6)
                throw new InvalidOperationException("Dia da semana inválido no horário.");

            var existing = tenant.OpeningHours.FirstOrDefault(h => (int)h.DayOfWeek == input.DayOfWeek);
            if (existing is null)
            {
                existing = new TenantOpeningHour { TenantId = tenant.Id, DayOfWeek = (DayOfWeek)input.DayOfWeek };
                tenant.OpeningHours.Add(existing);
            }

            existing.IsClosed = input.IsClosed;
            existing.OpenTime = ParseTime(input.OpenTime);
            existing.CloseTime = ParseTime(input.CloseTime);
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task SyncDeliveryZonesAsync(Tenant tenant, IReadOnlyList<DeliveryZoneInputDto> zones, CancellationToken ct)
    {
        var existingById = tenant.DeliveryZones.ToDictionary(z => z.Id);
        var keepIds = new HashSet<Guid>();

        foreach (var input in zones)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new InvalidOperationException("O nome da zona de entrega é obrigatório.");

            DeliveryZone zone;
            if (input.Id is Guid id && id != Guid.Empty && existingById.TryGetValue(id, out var existing))
            {
                zone = existing;
                keepIds.Add(id);
            }
            else
            {
                zone = new DeliveryZone
                {
                    TenantId = tenant.Id,
                    Name = input.Name.Trim(),
                    Id = input.Id is Guid newId && newId != Guid.Empty ? newId : Guid.NewGuid(),
                };
                await _unitOfWork.DeliveryZones.AddAsync(zone, ct);
                existingById[zone.Id] = zone;
                keepIds.Add(zone.Id);
            }

            zone.Name = input.Name.Trim();
            zone.Fee = Math.Max(0, input.Fee);
            zone.IsActive = input.IsActive;
            zone.SortOrder = input.SortOrder;
            zone.UpdatedAt = DateTimeOffset.UtcNow;
        }

        foreach (var removed in tenant.DeliveryZones.Where(z => !keepIds.Contains(z.Id)).ToList())
        {
            _unitOfWork.DeliveryZones.Remove(removed);
            tenant.DeliveryZones.Remove(removed);
        }
    }

    private static TimeOnly? ParseTime(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : TimeOnly.Parse(value);

    private static string? FormatTime(TimeOnly? value) => value?.ToString("HH:mm");

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");

    private static StoreSettingsDto Map(Tenant t) =>
        new(
            t.Id,
            t.Name,
            t.Description,
            t.LogoUrl,
            t.ContactEmail,
            t.ContactPhone,
            t.AddressLine,
            t.City,
            t.PostalCode,
            t.CustomDomain,
            t.ThemePrimaryColor,
            t.ThemeAccentColor,
            t.ThemeFontFamily,
            t.OrdersAcceptingEnabled,
            t.MinimumOrderAdvanceMinutes,
            t.PickupEnabled,
            t.HomeDeliveryEnabled,
            t.CemeteryDeliveryEnabled,
            t.OpeningHours.OrderBy(h => h.DayOfWeek).Select(h => new OpeningHourDto(
                (int)h.DayOfWeek,
                FormatTime(h.OpenTime),
                FormatTime(h.CloseTime),
                h.IsClosed)).ToList(),
            t.DeliveryZones.OrderBy(z => z.SortOrder).Select(z => new DeliveryZoneDto(
                z.Id, z.Name, z.Fee, z.IsActive, z.SortOrder)).ToList(),
            t.SeoTitle,
            t.SeoDescription);
}

public static class StoreSettingsValidation
{
    private static readonly UpdateStoreSettingsRequestValidator Validator = new();

    public static void EnsureValid(UpdateStoreSettingsRequest request)
    {
        var result = Validator.Validate(request);
        if (!result.IsValid)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }
}

public sealed class UpdateStoreSettingsRequestValidator : AbstractValidator<UpdateStoreSettingsRequest>
{
    public UpdateStoreSettingsRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.SeoTitle).MaximumLength(70);
        RuleFor(x => x.SeoDescription).MaximumLength(300);
        RuleFor(x => x.MinimumOrderAdvanceMinutes).GreaterThanOrEqualTo(0).LessThanOrEqualTo(60 * 24 * 14);
        RuleForEach(x => x.DeliveryZones).ChildRules(z =>
        {
            z.RuleFor(d => d.Name).NotEmpty().MaximumLength(120);
            z.RuleFor(d => d.Fee).GreaterThanOrEqualTo(0);
        });
    }
}
