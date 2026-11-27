using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Interfaces.Storage;
using FloristHub.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Invoicing;

public sealed class InvoiceService : IInvoiceService
{
    public const int MaxAttempts = 8;
    public const string NotConfiguredMessage = "A faturação desta loja ainda não está configurada.";
    private const string ReceiptPendingMessage = "A fatura-recibo ainda não foi emitida.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly IInvoicingProviderFactory _providers;
    private readonly ISecretProtector _secrets;
    private readonly IFileStorage _files;
    private readonly IAuditLogger _audit;
    private readonly ILogger<InvoiceService> _logger;
    private readonly InvoicingOptions _options;

    public InvoiceService(
        IUnitOfWork unitOfWork,
        ITenantContext tenant,
        IInvoicingProviderFactory providers,
        ISecretProtector secrets,
        IFileStorage files,
        IAuditLogger audit,
        IOptions<InvoicingOptions> options,
        ILogger<InvoiceService> logger)
    {
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _providers = providers;
        _secrets = secrets;
        _files = files;
        _audit = audit;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureInvoiceReceiptAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await LoadPaymentAsync(paymentId, ct);
        if (payment is null || payment.Status is not (PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded))
            return;

        await IssueAsync(
            payment,
            SalesDocumentKind.InvoiceReceipt,
            $"receipt:{payment.Id:N}",
            payment.Amount,
            fullLines: true,
            ct);
        if (payment.RefundedAmount > 0)
            await EnsureCreditNoteAsync(payment.Id, ct);
    }

    public async Task EnsureCreditNoteAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await LoadPaymentAsync(paymentId, ct);
        if (payment is null || payment.RefundedAmount <= 0)
            return;

        var issued = await Documents(payment.TenantId)
            .Where(d => d.PaymentId == payment.Id && d.Kind == SalesDocumentKind.CreditNote && d.Status == SalesDocumentStatus.Issued)
            .Select(d => d.GrossAmount)
            .ToListAsync(ct);
        var delta = payment.RefundedAmount - issued.Sum();
        if (delta <= 0.009m)
            return;

        await IssueAsync(
            payment,
            SalesDocumentKind.CreditNote,
            $"credit:{payment.Id:N}:{issued.Count + 1}",
            decimal.Round(delta, 2, MidpointRounding.AwayFromZero),
            fullLines: issued.Count == 0 && decimal.Round(delta, 2) == payment.Amount,
            ct);
    }

    public async Task<InvoicingSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        RequireTenant();
        var settings = await _unitOfWork.InvoicingSettings.Query().FirstOrDefaultAsync(ct);
        return settings is null ? InvoicingSettingsDto.Empty(_options.AllowFakeProvider == true) : Map(settings);
    }

    public async Task<InvoicingSettingsDto> UpdateSettingsAsync(UpdateInvoicingSettingsRequest request, Guid actorId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        Validate(request, _options.AllowFakeProvider == true);
        var settings = await _unitOfWork.InvoicingSettings.Query().FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new FloristInvoicingSettings { TenantId = tenantId };
            await _unitOfWork.InvoicingSettings.AddAsync(settings, ct);
        }

        settings.LegalName = request.LegalName.Trim();
        settings.TaxId = PortugueseNif.Normalize(request.TaxId);
        settings.AddressLine = request.AddressLine.Trim();
        settings.City = request.City.Trim();
        settings.PostalCode = request.PostalCode.Trim();
        settings.Country = string.IsNullOrWhiteSpace(request.Country) ? "Portugal" : request.Country.Trim();
        settings.Provider = request.Provider;
        settings.AccountName = string.IsNullOrWhiteSpace(request.AccountName) ? null : request.AccountName.Trim().ToLowerInvariant();
        settings.DocumentSeries = string.IsNullOrWhiteSpace(request.DocumentSeries) ? null : request.DocumentSeries.Trim();
        settings.DefaultVatRate = request.DefaultVatRate;
        settings.TaxExemptionCode = string.IsNullOrWhiteSpace(request.TaxExemptionCode) ? null : request.TaxExemptionCode.Trim().ToUpperInvariant();
        settings.Enabled = request.Enabled;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
            settings.ProtectedApiKey = _secrets.Protect(request.ApiKey.Trim());

        if (request.Enabled && request.Provider == InvoicingProviderKind.InvoiceXpress && string.IsNullOrWhiteSpace(settings.ProtectedApiKey))
            throw new InvalidOperationException("Indique a chave da API InvoiceXpress.");

        var stuck = await _unitOfWork.SalesDocuments.Query()
            .Where(d => d.Status != SalesDocumentStatus.Issued && d.LastError == NotConfiguredMessage)
            .ToListAsync(ct);
        foreach (var document in stuck)
        {
            document.AttemptCount = 0;
            document.Status = SalesDocumentStatus.Pending;
            document.LastError = null;
            document.NextAttemptAt = DateTimeOffset.UtcNow;
            document.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await _audit.LogAsync("InvoicingSettingsUpdated", actorId, tenantId, nameof(FloristInvoicingSettings), settings.Id, settings.Provider.ToString(), ct);
        return Map(settings);
    }

    public async Task<IReadOnlyList<SalesDocumentDto>> ListForFloristAsync(CancellationToken ct = default)
    {
        RequireTenant();
        var documents = await _unitOfWork.SalesDocuments.Query()
            .OrderByDescending(d => d.CreatedAt)
            .Take(200)
            .ToListAsync(ct);
        return documents.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SalesDocumentDto>> ListForOrderAsync(Guid orderId, Guid userId, string role, CancellationToken ct = default)
    {
        await RequireOrderAccessAsync(orderId, userId, role, ct);
        var documents = await _unitOfWork.SalesDocuments.Query()
            .Where(d => d.OrderId == orderId)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct);
        return documents.Select(Map).ToList();
    }

    public async Task<SalesDocumentDto> RetryAsync(Guid documentId, CancellationToken ct = default)
    {
        RequireTenant();
        var document = await _unitOfWork.SalesDocuments.Query().FirstOrDefaultAsync(d => d.Id == documentId, ct)
            ?? throw new KeyNotFoundException("Documento não encontrado.");
        document.NextAttemptAt = DateTimeOffset.UtcNow;
        if (document.AttemptCount >= MaxAttempts)
            document.AttemptCount = MaxAttempts - 1;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        if (document.Kind == SalesDocumentKind.CreditNote)
            await EnsureCreditNoteAsync(document.PaymentId, ct);
        else
            await EnsureInvoiceReceiptAsync(document.PaymentId, ct);
        var refreshed = await _unitOfWork.SalesDocuments.Query().FirstAsync(d => d.Id == documentId, ct);
        return Map(refreshed);
    }

    public async Task<InvoiceFile> OpenPdfAsync(Guid documentId, Guid userId, string role, CancellationToken ct = default)
    {
        var document = await _unitOfWork.SalesDocuments.Query().FirstOrDefaultAsync(d => d.Id == documentId, ct)
            ?? throw new KeyNotFoundException("Documento não encontrado.");
        await RequireOrderAccessAsync(document.OrderId, userId, role, ct);
        if (document.Status != SalesDocumentStatus.Issued)
            throw new InvalidOperationException("O documento ainda não foi emitido.");

        if (!string.IsNullOrWhiteSpace(document.PdfStoragePath))
        {
            var stored = await _files.OpenReadAsync(document.PdfStoragePath, ct);
            if (stored is not null)
            {
                await using (stored)
                {
                    using var buffer = new MemoryStream();
                    await stored.CopyToAsync(buffer, ct);
                    return new InvoiceFile(buffer.ToArray(), FileName(document));
                }
            }
        }

        var settings = await LoadSettingsAsync(document.TenantId, ct)
            ?? throw new InvalidOperationException("Faturação da loja não encontrada.");
        var pdf = await _providers.Get(document.Provider).DownloadPdfAsync(Session(settings, document.Kind), document.ProviderDocumentId ?? "", ct);
        document.PdfStoragePath = await StorePdfAsync(document.TenantId, pdf, ct);
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return new InvoiceFile(pdf, FileName(document));
    }

    public async Task RetryDueAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var due = await _unitOfWork.SalesDocuments.Query()
            .IgnoreQueryFilters()
            .Where(d => d.Status != SalesDocumentStatus.Issued
                && d.AttemptCount < MaxAttempts
                && (d.NextAttemptAt == null || d.NextAttemptAt <= now))
            .OrderBy(d => d.NextAttemptAt)
            .Take(20)
            .Select(d => new { d.PaymentId, d.Kind })
            .ToListAsync(ct);

        foreach (var item in due)
        {
            if (item.Kind == SalesDocumentKind.CreditNote)
                await EnsureCreditNoteAsync(item.PaymentId, ct);
            else
                await EnsureInvoiceReceiptAsync(item.PaymentId, ct);
        }
    }

    private async Task IssueAsync(
        Payment payment,
        SalesDocumentKind kind,
        string idempotencyKey,
        decimal gross,
        bool fullLines,
        CancellationToken ct)
    {
        var existing = await Documents(payment.TenantId)
            .FirstOrDefaultAsync(d => d.IdempotencyKey == idempotencyKey, ct);
        if (existing is { Status: SalesDocumentStatus.Issued })
            return;
        if (existing?.NextAttemptAt is DateTimeOffset next && next > DateTimeOffset.UtcNow && existing.Status == SalesDocumentStatus.Failed)
            return;

        var settings = await LoadSettingsAsync(payment.TenantId, ct);
        if (settings is null || !settings.Enabled)
        {
            await RememberFailureAsync(existing, payment, kind, idempotencyKey, gross, NotConfiguredMessage, TimeSpan.FromMinutes(15), ct);
            return;
        }

        if (settings.Provider == InvoicingProviderKind.Fake && _options.AllowFakeProvider != true)
        {
            await RememberFailureAsync(existing, payment, kind, idempotencyKey, gross, "O fornecedor de testes está desativado.", TimeSpan.FromHours(6), ct);
            return;
        }

        var order = await LoadOrderAsync(payment.OrderId, ct);
        if (order is null)
            return;

        SalesDocument? receipt = null;
        if (kind == SalesDocumentKind.CreditNote)
        {
            receipt = await Documents(payment.TenantId)
                .Where(d => d.PaymentId == payment.Id && d.Kind == SalesDocumentKind.InvoiceReceipt && d.Status == SalesDocumentStatus.Issued)
                .FirstOrDefaultAsync(ct);
            if (receipt is null)
            {
                await RememberFailureAsync(existing, payment, kind, idempotencyKey, gross, ReceiptPendingMessage, TimeSpan.FromMinutes(2), ct);
                return;
            }
        }

        var document = existing ?? new SalesDocument
        {
            TenantId = payment.TenantId,
            OrderId = payment.OrderId,
            PaymentId = payment.Id,
            Kind = kind,
            IdempotencyKey = idempotencyKey
        };
        if (existing is null)
            await _unitOfWork.SalesDocuments.AddAsync(document, ct);

        document.Provider = settings.Provider;
        document.GrossAmount = gross;
        document.CustomerTaxId = order.CustomerTaxId;
        document.AttemptCount += 1;
        document.Status = SalesDocumentStatus.Pending;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            var lines = BuildLines(order, gross, settings.DefaultVatRate, fullLines);
            if (lines.Any(l => l.VatRate == 0 && string.IsNullOrWhiteSpace(settings.TaxExemptionCode)))
                throw new InvalidOperationException("Há linhas isentas de IVA sem código de isenção na loja.");

            var totals = VatCalculator.Summarize(lines);
            document.NetAmount = totals.Net;
            document.VatAmount = totals.Vat;
            var provider = _providers.Get(settings.Provider);
            var session = Session(settings, kind);
            if (string.IsNullOrWhiteSpace(document.ProviderDocumentId))
            {
                var issued = await provider.IssueAsync(session, new InvoicingCall(
                    idempotencyKey,
                    kind,
                    receipt?.ProviderDocumentId,
                    settings.DocumentSeries,
                    lines.Any(l => l.VatRate == 0) ? settings.TaxExemptionCode : null,
                    Seller(settings),
                    Buyer(order),
                    lines), ct);
                document.ProviderDocumentId = issued.ProviderDocumentId;
                document.DocumentNumber = issued.DocumentNumber;
                document.Permalink = issued.Permalink;
                if (issued.Pdf is { Length: > 0 })
                    document.PdfStoragePath = await StorePdfAsync(payment.TenantId, issued.Pdf, ct);
            }

            if (string.IsNullOrWhiteSpace(document.PdfStoragePath))
            {
                var pdf = await provider.DownloadPdfAsync(session, document.ProviderDocumentId!, ct);
                document.PdfStoragePath = await StorePdfAsync(payment.TenantId, pdf, ct);
            }

            document.Status = SalesDocumentStatus.Issued;
            document.IssuedAt = DateTimeOffset.UtcNow;
            document.LastError = null;
            document.NextAttemptAt = null;
            document.UpdatedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            await _audit.LogAsync(
                kind == SalesDocumentKind.CreditNote ? "CreditNoteIssued" : "InvoiceReceiptIssued",
                null,
                payment.TenantId,
                nameof(SalesDocument),
                document.Id,
                document.DocumentNumber,
                ct);
        }
        catch (InvoicingPartialException ex)
        {
            document.ProviderDocumentId ??= ex.ProviderDocumentId;
            await MarkFailedAsync(document, ex.Message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao emitir documento {DocumentId} da loja {TenantId}", document.Id, payment.TenantId);
            await MarkFailedAsync(document, ex.Message, ct);
        }
    }

    private async Task RememberFailureAsync(
        SalesDocument? existing,
        Payment payment,
        SalesDocumentKind kind,
        string idempotencyKey,
        decimal gross,
        string message,
        TimeSpan delay,
        CancellationToken ct)
    {
        var document = existing ?? new SalesDocument
        {
            TenantId = payment.TenantId,
            OrderId = payment.OrderId,
            PaymentId = payment.Id,
            Kind = kind,
            IdempotencyKey = idempotencyKey
        };
        if (existing is null)
            await _unitOfWork.SalesDocuments.AddAsync(document, ct);
        document.GrossAmount = gross;
        document.AttemptCount += 1;
        document.Status = SalesDocumentStatus.Failed;
        document.LastError = Trim(message);
        document.NextAttemptAt = DateTimeOffset.UtcNow.Add(delay);
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task MarkFailedAsync(SalesDocument document, string message, CancellationToken ct)
    {
        document.Status = SalesDocumentStatus.Failed;
        document.LastError = Trim(message);
        document.NextAttemptAt = DateTimeOffset.UtcNow.Add(Backoff(document.AttemptCount));
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static TimeSpan Backoff(int attempt)
    {
        var minutes = Math.Min(60, Math.Pow(2, Math.Max(0, attempt - 1)));
        return TimeSpan.FromMinutes(minutes);
    }

    internal static IReadOnlyList<InvoiceLine> BuildLines(Order order, decimal gross, decimal defaultVat, bool fullLines)
    {
        if (!VatRates.IsAllowed(defaultVat))
            defaultVat = VatRates.StandardMainland;
        if (!fullLines)
            return [new InvoiceLine("Reembolso", "Nota de crédito", 1, gross, defaultVat)];

        var merchandise = order.Items.Sum(i => decimal.Round(i.UnitPrice * i.Quantity, 2, MidpointRounding.AwayFromZero));
        var discount = decimal.Round(order.DiscountAmount, 2, MidpointRounding.AwayFromZero);
        var expected = decimal.Round(merchandise + order.DeliveryFee - discount, 2, MidpointRounding.AwayFromZero);
        if (order.Items.Count == 0 || Math.Abs(expected - gross) > 0.05m)
            return [new InvoiceLine("Encomenda", "Arranjo floral", 1, gross, defaultVat)];

        var lines = order.Items
            .Select(item => new InvoiceLine(item.Name, null, item.Quantity, item.UnitPrice, VatRates.IsAllowed(item.VatRate) ? item.VatRate : defaultVat))
            .ToList();
        if (order.DeliveryFee > 0)
            lines.Add(new InvoiceLine("Entrega", order.DeliveryZone?.Name, 1, order.DeliveryFee, defaultVat));
        if (discount > 0)
            lines.Add(new InvoiceLine(
                string.IsNullOrWhiteSpace(order.CouponCode) ? "Desconto" : "Desconto " + order.CouponCode,
                "Cupão",
                1,
                -discount,
                defaultVat));
        return lines;
    }

    private static SellerProfile Seller(FloristInvoicingSettings settings) =>
        new(settings.LegalName, settings.TaxId, settings.AddressLine, settings.City, settings.PostalCode, settings.Country);

    private static BuyerProfile Buyer(Order order)
    {
        var name = string.IsNullOrWhiteSpace(order.Customer?.FullName) ? "Consumidor final" : order.Customer.FullName.Trim();
        var code = order.CustomerTaxId ?? ("C" + order.CustomerId.ToString("N")[..12]);
        return new BuyerProfile(name, order.Customer?.Email, order.CustomerTaxId, code);
    }

    private ProviderSession Session(FloristInvoicingSettings settings, SalesDocumentKind kind)
    {
        string? apiKey = null;
        if (!string.IsNullOrWhiteSpace(settings.ProtectedApiKey))
            apiKey = _secrets.Unprotect(settings.ProtectedApiKey);
        return new ProviderSession(settings.AccountName, apiKey, kind);
    }

    private async Task<string> StorePdfAsync(Guid tenantId, byte[] pdf, CancellationToken ct)
    {
        await using var stream = new MemoryStream(pdf);
        var stored = await _files.SaveAsync(stream, "application/pdf", "fatura.pdf", tenantId, ct);
        return stored.StoragePath;
    }

    private IQueryable<SalesDocument> Documents(Guid tenantId) =>
        _unitOfWork.SalesDocuments.Query().IgnoreQueryFilters().Where(d => d.TenantId == tenantId);

    private async Task<Payment?> LoadPaymentAsync(Guid paymentId, CancellationToken ct) =>
        await _unitOfWork.Payments.Query().IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == paymentId, ct);

    private async Task<Order?> LoadOrderAsync(Guid orderId, CancellationToken ct) =>
        await _unitOfWork.Orders.Query()
            .IgnoreQueryFilters()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.DeliveryZone)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

    private async Task<FloristInvoicingSettings?> LoadSettingsAsync(Guid tenantId, CancellationToken ct) =>
        await _unitOfWork.InvoicingSettings.Query().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

    private async Task RequireOrderAccessAsync(Guid orderId, Guid userId, string role, CancellationToken ct)
    {
        var order = await _unitOfWork.Orders.Query().IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new KeyNotFoundException("Encomenda não encontrada.");
        if (role == DAL.Constants.Roles.Florist)
        {
            if (_tenant.TenantId != order.TenantId)
                throw new UnauthorizedAccessException("Sem acesso a este documento.");
            return;
        }

        if (order.CustomerId != userId || (_tenant.TenantId is Guid tenant && tenant != order.TenantId))
            throw new UnauthorizedAccessException("Sem acesso a este documento.");
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");

    private static void Validate(UpdateInvoicingSettingsRequest request, bool allowFake)
    {
        if (string.IsNullOrWhiteSpace(request.LegalName) || request.LegalName.Trim().Length > 200)
            throw new InvalidOperationException("Indique o nome legal da florista.");
        PortugueseNif.Normalize(request.TaxId);
        if (string.IsNullOrWhiteSpace(request.AddressLine) || string.IsNullOrWhiteSpace(request.City))
            throw new InvalidOperationException("Indique a morada e a localidade.");
        if (string.IsNullOrWhiteSpace(request.PostalCode) || !System.Text.RegularExpressions.Regex.IsMatch(request.PostalCode.Trim(), @"^\d{4}-\d{3}$"))
            throw new InvalidOperationException("Código postal inválido. Use o formato 0000-000.");
        if (!VatRates.IsAllowed(request.DefaultVatRate))
            throw new InvalidOperationException("Taxa de IVA por defeito inválida.");
        if (request.DefaultVatRate == 0 && string.IsNullOrWhiteSpace(request.TaxExemptionCode))
            throw new InvalidOperationException("Indique o código de isenção de IVA.");
        if (!string.IsNullOrWhiteSpace(request.TaxExemptionCode) &&
            !System.Text.RegularExpressions.Regex.IsMatch(request.TaxExemptionCode.Trim(), "^M[0-9]{2}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Código de isenção inválido.");
        if (request.Provider == InvoicingProviderKind.Fake && !allowFake)
            throw new InvalidOperationException("O fornecedor de testes não pode ser usado em produção.");
        if (request.Provider == InvoicingProviderKind.InvoiceXpress)
        {
            InvoiceXpressInvoicingProvider.RequireAccount(request.AccountName);
            if (request.Enabled && string.IsNullOrWhiteSpace(request.ApiKey))
            {
                // A chave já guardada é verificada depois.
            }
            else if (!string.IsNullOrWhiteSpace(request.ApiKey) && request.ApiKey.Trim().Length is < 8 or > 200)
                throw new InvalidOperationException("Chave da API inválida.");
        }

        if (!string.IsNullOrWhiteSpace(request.DocumentSeries) && request.DocumentSeries.Trim().Length > 40)
            throw new InvalidOperationException("A série é demasiado longa.");
    }

    private static InvoicingSettingsDto Map(FloristInvoicingSettings settings) =>
        new(
            settings.LegalName,
            settings.TaxId,
            settings.AddressLine,
            settings.City,
            settings.PostalCode,
            settings.Country,
            settings.Provider,
            settings.AccountName,
            settings.DocumentSeries,
            settings.DefaultVatRate,
            settings.TaxExemptionCode,
            settings.Enabled,
            !string.IsNullOrWhiteSpace(settings.ProtectedApiKey));

    private static SalesDocumentDto Map(SalesDocument document) =>
        new(
            document.Id,
            document.OrderId,
            document.PaymentId,
            document.Kind,
            document.Status,
            document.DocumentNumber,
            document.Permalink,
            document.GrossAmount,
            document.CustomerTaxId,
            document.LastError,
            document.IssuedAt,
            document.CreatedAt);

    private static string FileName(SalesDocument document)
    {
        var raw = string.IsNullOrWhiteSpace(document.DocumentNumber) ? document.Id.ToString("N") : document.DocumentNumber;
        var safe = new string(raw.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-').ToArray());
        return $"{safe}.pdf";
    }

    private static string Trim(string message)
    {
        var text = message.ReplaceLineEndings(" ").Trim();
        return text.Length <= 1000 ? text : text[..1000];
    }
}
