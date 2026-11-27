using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

/// <summary>
/// Dados fiscais da florista e credenciais do software certificado. A chave da API fica cifrada.
/// </summary>
public class FloristInvoicingSettings : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string LegalName { get; set; } = "";
    public string TaxId { get; set; } = "";
    public string AddressLine { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "Portugal";
    public InvoicingProviderKind Provider { get; set; } = InvoicingProviderKind.Fake;
    public string? AccountName { get; set; }
    public string? ProtectedApiKey { get; set; }
    public string? DocumentSeries { get; set; }
    public decimal DefaultVatRate { get; set; } = 23m;
    public string? TaxExemptionCode { get; set; }
    public bool Enabled { get; set; }
}
