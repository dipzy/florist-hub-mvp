namespace FloristHub.Services.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string Role,
    Guid? TenantId,
    string? NewTenantName,
    string? NewTenantSlug,
    bool AcceptTermsAndPrivacy,
    bool MarketingConsent,
    string? TermsVersion,
    string? PrivacyVersion);

public record LoginRequest(string Email, string Password, Guid? TenantId, string? TenantSlug);

public record CompleteFloristInviteRequest(Guid UserId, string Token, string NewPassword);

public static class AuthContextKinds
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Florist = "Florist";
}

public record AuthContextOption(
    string Kind,
    Guid? TenantId,
    string? TenantName,
    string? TenantSlug);

public record SwitchContextRequest(string Kind, Guid? TenantId);

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    Guid UserId,
    string Email,
    string Role,
    Guid? TenantId,
    string? TenantSlug,
    IReadOnlyList<AuthContextOption>? AvailableContexts = null);
