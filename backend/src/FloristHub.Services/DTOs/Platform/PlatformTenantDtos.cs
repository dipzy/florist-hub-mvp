namespace FloristHub.Services.DTOs.Platform;

public record PlatformTenantListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? CustomDomain,
    bool IsActive,
    bool IsPublished,
    int OrderCount,
    DateTimeOffset CreatedAt);

public record PlatformTenantDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? CustomDomain,
    string? ContactEmail,
    string? ContactPhone,
    bool IsActive,
    bool IsPublished,
    string? OnboardingWizardStep,
    int OrderCount,
    string ThemePrimaryColor,
    string ThemeAccentColor,
    string? ThemeFontFamily,
    DateTimeOffset CreatedAt);

public record CreatePlatformTenantRequest(
    string Name,
    string Slug,
    string FloristEmail,
    string FloristFullName,
    string? CustomDomain);

public record CreatePlatformTenantResponse(
    PlatformTenantDetailDto Tenant,
    string? DevInviteLink,
    bool FloristInviteSent);

public record ResendFloristInviteResponse(string? DevInviteLink);

public record UpdatePlatformTenantRequest(
    string Name,
    string? CustomDomain,
    string ThemePrimaryColor,
    string ThemeAccentColor,
    string? ThemeFontFamily);

public record SuspendPlatformTenantRequest(bool Suspended);
