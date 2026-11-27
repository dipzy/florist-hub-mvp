using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Store;
using FloristHub.Services.Onboarding;

namespace FloristHub.Services;

public interface IStoreCheckoutInfoService
{
    Task<PublicStoreCheckoutDto> GetAsync(CancellationToken ct = default);
}

public class StoreCheckoutInfoService : IStoreCheckoutInfoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;

    public StoreCheckoutInfoService(IUnitOfWork unitOfWork, ITenantContext tenant)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
    }

    public async Task<PublicStoreCheckoutDto> GetAsync(CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Selecione uma loja (tenant).");
        var tenant = await _unitOfWork.Tenants.GetByIdWithSettingsReadOnlyAsync(tenantId, ct)
            ?? throw new KeyNotFoundException("Loja não encontrada.");

        TenantPublicAccessRules.EnsurePublicStoreOpen(tenant);

        return new PublicStoreCheckoutDto(
            tenant.Name,
            tenant.LogoUrl,
            tenant.OrdersAcceptingEnabled,
            tenant.MinimumOrderAdvanceMinutes,
            tenant.PickupEnabled,
            tenant.HomeDeliveryEnabled,
            tenant.CemeteryDeliveryEnabled,
            tenant.OpeningHours
                .OrderBy(h => h.DayOfWeek)
                .Select(h => new OpeningHourDto(
                    (int)h.DayOfWeek,
                    h.OpenTime?.ToString("HH:mm"),
                    h.CloseTime?.ToString("HH:mm"),
                    h.IsClosed))
                .ToList(),
            tenant.DeliveryZones
                .Where(z => z.IsActive)
                .OrderBy(z => z.SortOrder)
                .Select(z => new DeliveryZoneDto(z.Id, z.Name, z.Fee, z.IsActive, z.SortOrder))
                .ToList());
    }
}
