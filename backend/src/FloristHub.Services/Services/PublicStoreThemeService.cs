using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Store;
using FloristHub.Services.Onboarding;

namespace FloristHub.Services;

public interface IPublicStoreThemeService
{
    Task<PublicStoreThemeDto> GetAsync(CancellationToken ct = default);
}

public class PublicStoreThemeService : IPublicStoreThemeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;

    public PublicStoreThemeService(IUnitOfWork unitOfWork, ITenantContext tenant)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
    }

    public async Task<PublicStoreThemeDto> GetAsync(CancellationToken ct = default)
    {
        var tenant = await TenantPublicAccessRules.RequirePublishedTenantAsync(_tenant, _unitOfWork, ct);

        return new PublicStoreThemeDto(
            tenant.Name,
            tenant.Slug,
            tenant.ThemePrimaryColor,
            tenant.ThemeAccentColor,
            tenant.ThemeFontFamily);
    }
}
