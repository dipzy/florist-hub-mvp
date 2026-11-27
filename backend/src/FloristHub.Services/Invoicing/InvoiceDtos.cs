using FloristHub.DAL.Enums;

namespace FloristHub.Services.Invoicing;

public interface IInvoiceIssuance
{
    Task EnsureInvoiceReceiptAsync(Guid paymentId, CancellationToken ct = default);
    Task EnsureCreditNoteAsync(Guid paymentId, CancellationToken ct = default);
}

public interface IInvoiceService : IInvoiceIssuance
{
    Task<InvoicingSettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task<InvoicingSettingsDto> UpdateSettingsAsync(UpdateInvoicingSettingsRequest request, Guid actorId, CancellationToken ct = default);
    Task<IReadOnlyList<SalesDocumentDto>> ListForFloristAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SalesDocumentDto>> ListForOrderAsync(Guid orderId, Guid userId, string role, CancellationToken ct = default);
    Task<SalesDocumentDto> RetryAsync(Guid documentId, CancellationToken ct = default);
    Task<InvoiceFile> OpenPdfAsync(Guid documentId, Guid userId, string role, CancellationToken ct = default);
    Task RetryDueAsync(CancellationToken ct = default);
}

public record InvoicingSettingsDto(
    string LegalName,
    string TaxId,
    string AddressLine,
    string City,
    string PostalCode,
    string Country,
    InvoicingProviderKind Provider,
    string? AccountName,
    string? DocumentSeries,
    decimal DefaultVatRate,
    string? TaxExemptionCode,
    bool Enabled,
    bool CredentialsConfigured)
{
    public static InvoicingSettingsDto Empty(bool allowFake) => new(
        "",
        "",
        "",
        "",
        "",
        "Portugal",
        allowFake ? InvoicingProviderKind.Fake : InvoicingProviderKind.InvoiceXpress,
        null,
        null,
        VatRates.StandardMainland,
        null,
        false,
        false);
}

public record UpdateInvoicingSettingsRequest(
    string LegalName,
    string TaxId,
    string AddressLine,
    string City,
    string PostalCode,
    string? Country,
    InvoicingProviderKind Provider,
    string? AccountName,
    string? ApiKey,
    string? DocumentSeries,
    decimal DefaultVatRate,
    string? TaxExemptionCode,
    bool Enabled);

public record SalesDocumentDto(
    Guid Id,
    Guid OrderId,
    Guid PaymentId,
    SalesDocumentKind Kind,
    SalesDocumentStatus Status,
    string? DocumentNumber,
    string? Permalink,
    decimal GrossAmount,
    string? CustomerTaxId,
    string? LastError,
    DateTimeOffset? IssuedAt,
    DateTimeOffset CreatedAt);

public sealed record InvoiceFile(byte[] Content, string FileName);
