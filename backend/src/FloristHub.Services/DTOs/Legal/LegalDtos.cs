using FloristHub.DAL.Enums;

namespace FloristHub.Services.DTOs.Legal;

public record LegalVersionsDto(
    string TermsVersion,
    string PrivacyVersion,
    string CookiesVersion);

public record PublicLegalDocumentDto(
    LegalDocumentType DocumentType,
    string Version,
    string Title,
    string ContentMarkdown,
    string? ResponsibleSectionMarkdown);

public record LegalConsentRequest(
    bool AcceptTermsAndPrivacy,
    string TermsVersion,
    string PrivacyVersion);

public record CustomerDataExportDto(
    Guid UserId,
    string? Email,
    string? FullName,
    bool MarketingConsent,
    DateTimeOffset? MarketingConsentAt,
    string? AcceptedTermsVersion,
    DateTimeOffset? AcceptedTermsAt,
    string? AcceptedPrivacyVersion,
    DateTimeOffset? AcceptedPrivacyAt,
    IReadOnlyList<CustomerOrderExportDto> Orders,
    DateTimeOffset ExportedAt);

public record CustomerOrderExportDto(
    Guid OrderId,
    DateTimeOffset CreatedAt,
    int Status,
    decimal EstimatedTotal,
    decimal DeliveryFee,
    string? DeliveryAddress,
    string? CustomerNotes,
    IReadOnlyList<CustomerOrderItemExportDto> Items);

public record CustomerOrderItemExportDto(string Name, int Quantity, decimal UnitPrice);

public record ComplianceAuditEntryDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid? TenantId,
    string? TenantName,
    Guid? UserId,
    string Action,
    string? Details);

public record ComplianceAuditQuery(int Page = 1, int PageSize = 50);
