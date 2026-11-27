using FloristHub.DAL.Common;
using Microsoft.AspNetCore.Identity;

namespace FloristHub.DAL.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? AcceptedTermsVersion { get; set; }
    public DateTimeOffset? AcceptedTermsAt { get; set; }
    public string? AcceptedPrivacyVersion { get; set; }
    public DateTimeOffset? AcceptedPrivacyAt { get; set; }
    public bool MarketingConsent { get; set; }
    public DateTimeOffset? MarketingConsentAt { get; set; }
    public bool IsAnonymized { get; set; }
    public DateTimeOffset? AnonymizedAt { get; set; }
}
