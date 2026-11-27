namespace FloristHub.Services.DTOs.Store;

public record OpeningHourDto(int DayOfWeek, string? OpenTime, string? CloseTime, bool IsClosed);

public record DeliveryZoneDto(Guid Id, string Name, decimal Fee, bool IsActive, int SortOrder);

public record StoreSettingsDto(
    Guid TenantId,
    string Name,
    string? Description,
    string? LogoUrl,
    /// <summary>Contacto público e destino das notificações de novas encomendas para a florista.</summary>
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine,
    string? City,
    string? PostalCode,
    string? CustomDomain,
    string ThemePrimaryColor,
    string ThemeAccentColor,
    string? ThemeFontFamily,
    bool OrdersAcceptingEnabled,
    int MinimumOrderAdvanceMinutes,
    bool PickupEnabled,
    bool HomeDeliveryEnabled,
    bool CemeteryDeliveryEnabled,
    IReadOnlyList<OpeningHourDto> OpeningHours,
    IReadOnlyList<DeliveryZoneDto> DeliveryZones,
    string? SeoTitle = null,
    string? SeoDescription = null);

public record UpdateStoreSettingsRequest(
    string Name,
    string? Description,
    string? LogoUrl,
    /// <summary>Contacto público e destino das notificações de novas encomendas para a florista.</summary>
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine,
    string? City,
    string? PostalCode,
    string ThemePrimaryColor,
    string ThemeAccentColor,
    string? ThemeFontFamily,
    bool OrdersAcceptingEnabled,
    int MinimumOrderAdvanceMinutes,
    bool PickupEnabled,
    bool HomeDeliveryEnabled,
    bool CemeteryDeliveryEnabled,
    IReadOnlyList<OpeningHourDto> OpeningHours,
    IReadOnlyList<DeliveryZoneInputDto> DeliveryZones,
    string? SeoTitle = null,
    string? SeoDescription = null);

public record DeliveryZoneInputDto(Guid? Id, string Name, decimal Fee, bool IsActive, int SortOrder);

/// <summary>Informação pública para o fluxo de encomenda (sem contactos sensíveis da florista).</summary>
public record PublicStoreThemeDto(
    string Name,
    string Slug,
    string ThemePrimaryColor,
    string ThemeAccentColor,
    string? ThemeFontFamily);

public record PublicStoreCheckoutDto(
    string Name,
    string? LogoUrl,
    bool OrdersAcceptingEnabled,
    int MinimumOrderAdvanceMinutes,
    bool PickupEnabled,
    bool HomeDeliveryEnabled,
    bool CemeteryDeliveryEnabled,
    IReadOnlyList<OpeningHourDto> OpeningHours,
    IReadOnlyList<DeliveryZoneDto> DeliveryZones);
