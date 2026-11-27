using FloristHub.DAL.Common;
using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

/// <summary>
/// Fatura-recibo ou nota de crédito emitida pelo fornecedor certificado, em nome da florista.
/// </summary>
public class SalesDocument : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public SalesDocumentKind Kind { get; set; }
    public SalesDocumentStatus Status { get; set; } = SalesDocumentStatus.Pending;
    public string IdempotencyKey { get; set; } = "";
    public InvoicingProviderKind Provider { get; set; }
    public string? ProviderDocumentId { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Permalink { get; set; }
    public string? PdfStoragePath { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public string? CustomerTaxId { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
}
