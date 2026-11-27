using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Auth;
using FloristHub.Services.DTOs.Legal;
using FloristHub.Services.Legal;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services;

public interface ILegalComplianceService
{
    Task<LegalVersionsDto> GetCurrentVersionsAsync(CancellationToken ct = default);
    Task<PublicLegalDocumentDto> GetDocumentAsync(LegalDocumentType type, CancellationToken ct = default);
    void EnsureCheckoutConsent(LegalConsentRequest consent, LegalVersionsDto current);
    Task RecordRegistrationConsentsAsync(ApplicationUser user, RegisterRequest request, CancellationToken ct = default);
    Task RecordOrderConsentAsync(Order order, LegalConsentRequest consent, Guid userId, CancellationToken ct = default);
    Task RecordPaymentConsentAsync(Order order, LegalConsentRequest consent, Guid userId, CancellationToken ct = default);
}

public class LegalComplianceService : ILegalComplianceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;

    public LegalComplianceService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<LegalVersionsDto> GetCurrentVersionsAsync(CancellationToken ct = default)
    {
        var docs = await _unitOfWork.LegalDocumentVersions.Query()
            .AsNoTracking()
            .Where(d => d.IsCurrent)
            .ToListAsync(ct);

        string VersionFor(LegalDocumentType type) =>
            docs.FirstOrDefault(d => d.DocumentType == type)?.Version
            ?? throw new InvalidOperationException($"Versão legal em falta para {type}.");

        return new LegalVersionsDto(
            VersionFor(LegalDocumentType.Terms),
            VersionFor(LegalDocumentType.Privacy),
            VersionFor(LegalDocumentType.Cookies));
    }

    public async Task<PublicLegalDocumentDto> GetDocumentAsync(LegalDocumentType type, CancellationToken ct = default)
    {
        var doc = await _unitOfWork.LegalDocumentVersions.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentType == type && d.IsCurrent, ct)
            ?? throw new KeyNotFoundException("Documento legal não encontrado.");

        Tenant? tenant = null;
        FloristInvoicingSettings? invoicing = null;
        if (_tenant.TenantId is Guid tenantId)
        {
            tenant = await _unitOfWork.Tenants.GetByIdReadOnlyAsync(tenantId, ct);
            invoicing = await _unitOfWork.InvoicingSettings.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
        }

        return new PublicLegalDocumentDto(
            type,
            doc.Version,
            doc.Title,
            doc.ContentMarkdown,
            tenant is null ? null : BuildResponsibleSection(tenant, invoicing));
    }

    public void EnsureCheckoutConsent(LegalConsentRequest consent, LegalVersionsDto current)
    {
        if (!consent.AcceptTermsAndPrivacy)
            throw new InvalidOperationException("Tem de aceitar os Termos e a Política de Privacidade.");

        if (!string.Equals(consent.TermsVersion, current.TermsVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Versão dos Termos desatualizada. Atualize a página e aceite novamente.");

        if (!string.Equals(consent.PrivacyVersion, current.PrivacyVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Versão da Política de Privacidade desatualizada. Atualize a página e aceite novamente.");
    }

    public async Task RecordRegistrationConsentsAsync(ApplicationUser user, RegisterRequest request, CancellationToken ct = default)
    {
        var current = await GetCurrentVersionsAsync(ct);
        var consent = new LegalConsentRequest(
            request.AcceptTermsAndPrivacy,
            request.TermsVersion?.Trim() ?? string.Empty,
            request.PrivacyVersion?.Trim() ?? string.Empty);
        EnsureCheckoutConsent(consent, current);

        var now = DateTimeOffset.UtcNow;
        user.AcceptedTermsVersion = current.TermsVersion;
        user.AcceptedTermsAt = now;
        user.AcceptedPrivacyVersion = current.PrivacyVersion;
        user.AcceptedPrivacyAt = now;
        user.MarketingConsent = request.MarketingConsent;
        user.MarketingConsentAt = request.MarketingConsent ? now : null;

        var tenantId = user.TenantId;
        await _audit.LogAsync(
            ComplianceAuditActions.TermsAcceptedRegistration,
            user.Id,
            tenantId,
            nameof(ApplicationUser),
            user.Id,
            $"v={current.TermsVersion}",
            ct);
        await _audit.LogAsync(
            ComplianceAuditActions.PrivacyAcceptedRegistration,
            user.Id,
            tenantId,
            nameof(ApplicationUser),
            user.Id,
            $"v={current.PrivacyVersion}",
            ct);
        if (request.MarketingConsent)
        {
            await _audit.LogAsync(
                ComplianceAuditActions.MarketingConsentRegistration,
                user.Id,
                tenantId,
                nameof(ApplicationUser),
                user.Id,
                "opt-in",
                ct);
        }
    }

    public async Task RecordOrderConsentAsync(Order order, LegalConsentRequest consent, Guid userId, CancellationToken ct = default)
    {
        var current = await GetCurrentVersionsAsync(ct);
        EnsureCheckoutConsent(consent, current);
        var now = DateTimeOffset.UtcNow;
        order.AcceptedTermsVersion = current.TermsVersion;
        order.AcceptedPrivacyVersion = current.PrivacyVersion;
        order.LegalConsentRecordedAt = now;
        await _audit.LogAsync(
            ComplianceAuditActions.OrderLegalConsent,
            userId,
            order.TenantId,
            nameof(Order),
            order.Id,
            $"terms={current.TermsVersion};privacy={current.PrivacyVersion}",
            ct);
    }

    public async Task RecordPaymentConsentAsync(Order order, LegalConsentRequest consent, Guid userId, CancellationToken ct = default)
    {
        var current = await GetCurrentVersionsAsync(ct);
        EnsureCheckoutConsent(consent, current);
        order.AcceptedTermsVersion = current.TermsVersion;
        order.AcceptedPrivacyVersion = current.PrivacyVersion;
        order.LegalConsentRecordedAt = DateTimeOffset.UtcNow;
        await _audit.LogAsync(
            ComplianceAuditActions.PaymentLegalConsent,
            userId,
            order.TenantId,
            nameof(Order),
            order.Id,
            $"terms={current.TermsVersion};privacy={current.PrivacyVersion}",
            ct);
    }

    internal static string BuildResponsibleSection(Tenant tenant, FloristInvoicingSettings? invoicing)
    {
        var lines = new List<string>
        {
            "## Responsável pelo tratamento (loja)",
            "",
        };

        var legalName = invoicing?.LegalName?.Trim();
        if (!string.IsNullOrWhiteSpace(legalName))
            lines.Add($"- **Nome / denominação:** {legalName}");
        else
            lines.Add($"- **Nome comercial:** {tenant.Name}");

        if (!string.IsNullOrWhiteSpace(invoicing?.TaxId))
            lines.Add($"- **NIF:** {invoicing.TaxId.Trim()}");

        var addressParts = invoicing is not null
            ? new[] { invoicing.AddressLine, invoicing.PostalCode, invoicing.City }
            : new[] { tenant.AddressLine, tenant.PostalCode, tenant.City };
        var address = string.Join(
            ", ",
            addressParts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
        if (!string.IsNullOrWhiteSpace(address))
            lines.Add($"- **Morada:** {address}");

        var contact = tenant.ContactEmail ?? tenant.ContactPhone;
        if (!string.IsNullOrWhiteSpace(contact))
            lines.Add($"- **Contacto:** {contact!.Trim()}");

        if (lines.Count <= 2)
            lines.Add("_A florista ainda não preencheu os dados legais completos nesta loja._");

        return string.Join('\n', lines);
    }
}
