using FloristHub.DAL.Enums;

namespace FloristHub.Services.Invoicing;

public sealed class InvoicingOptions
{
    public const string SectionName = "Invoicing";
    public int RetryIntervalSeconds { get; set; } = 60;
    public bool? AllowFakeProvider { get; set; }
}

public sealed record SellerProfile(
    string LegalName,
    string TaxId,
    string AddressLine,
    string City,
    string PostalCode,
    string Country);

public sealed record BuyerProfile(string Name, string? Email, string? TaxId, string Code);

public sealed record InvoicingCall(
    string IdempotencyKey,
    SalesDocumentKind Kind,
    string? OwnerProviderDocumentId,
    string? SequenceId,
    string? TaxExemptionCode,
    SellerProfile Seller,
    BuyerProfile Buyer,
    IReadOnlyList<InvoiceLine> Lines);

public sealed record ProviderSession(string? AccountName, string? ApiKey, SalesDocumentKind Kind);

public sealed record ProviderIssuedDocument(
    string ProviderDocumentId,
    string DocumentNumber,
    string? Permalink,
    byte[]? Pdf);

public interface IInvoicingProvider
{
    InvoicingProviderKind Kind { get; }
    Task<ProviderIssuedDocument> IssueAsync(ProviderSession session, InvoicingCall call, CancellationToken ct);
    Task<byte[]> DownloadPdfAsync(ProviderSession session, string providerDocumentId, CancellationToken ct);
}

public interface IInvoicingProviderFactory
{
    IInvoicingProvider Get(InvoicingProviderKind kind);
}

public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
