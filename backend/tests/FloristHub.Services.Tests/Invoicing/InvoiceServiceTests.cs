using System.Net;
using System.Text;
using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.Invoicing;
using FloristHub.Services.Interfaces.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Tests.Invoicing;

public class InvoiceServiceTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");

    [Fact]
    public void Portuguese_nif_accepts_checksum_and_rejects_the_rest()
    {
        Assert.True(PortugueseNif.IsValid("123456789"));
        Assert.True(PortugueseNif.IsValid("PT 123 456 789"));
        Assert.Equal("123456789", PortugueseNif.Normalize("pt123456789"));
        Assert.False(PortugueseNif.IsValid("123456780"));
        Assert.False(PortugueseNif.IsValid("12345678"));
        Assert.Equal(10m, VatCalculator.NetUnit(12.30m, 23m));
    }

    [Fact]
    public async Task Paid_order_issues_one_receipt_and_a_second_call_does_not_duplicate_it()
    {
        var fx = Create();
        var order = await SeedOrderAsync(fx, TenantA, 12.30m, "123456789", 6m);
        var payment = await SeedPaymentAsync(fx, order);
        await fx.Invoices.EnsureInvoiceReceiptAsync(payment.Id);
        await fx.Invoices.EnsureInvoiceReceiptAsync(payment.Id);

        var documents = await fx.Db.SalesDocuments.IgnoreQueryFilters().ToListAsync();
        var receipt = Assert.Single(documents);
        Assert.Equal(SalesDocumentStatus.Issued, receipt.Status);
        Assert.Equal(TenantA, receipt.TenantId);
        Assert.Equal("123456789", receipt.CustomerTaxId);
        Assert.Equal(1, fx.Provider.IssueCalls);
        Assert.Contains("FR TEST", receipt.DocumentNumber);
        var pdf = await fx.Files.OpenReadAsync(receipt.PdfStoragePath!);
        Assert.NotNull(pdf);
        using var buffer = new MemoryStream();
        await pdf.CopyToAsync(buffer);
        var text = Encoding.ASCII.GetString(buffer.ToArray());
        Assert.Contains("500000000", text);
        Assert.Contains("123456789", text);
    }

    [Fact]
    public async Task Credentials_are_encrypted_and_absent_from_the_settings_dto()
    {
        var fx = Create();
        var saved = await fx.Invoices.UpdateSettingsAsync(Settings(apiKey: "super-secret-key"), Guid.NewGuid());
        var stored = await fx.Db.FloristInvoicingSettings.IgnoreQueryFilters().SingleAsync(s => s.TenantId == TenantA);

        Assert.True(saved.CredentialsConfigured);
        Assert.DoesNotContain("super-secret-key", System.Text.Json.JsonSerializer.Serialize(saved), StringComparison.Ordinal);
        Assert.Equal("enc:super-secret-key", stored.ProtectedApiKey);
        Assert.NotEqual("super-secret-key", stored.ProtectedApiKey);
    }

    [Fact]
    public async Task Invalid_customer_nif_is_rejected_before_checkout_changes_the_order()
    {
        var fx = Create();
        var order = await SeedOrderAsync(fx, TenantA, 10m, null, 23m);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Invoices.UpdateSettingsAsync(Settings(taxId: "123456780"), Guid.NewGuid()));
        Assert.False(PortugueseNif.IsValid("123456780"));
        order.CustomerTaxId = "123456780";
        Assert.False(PortugueseNif.IsValid(order.CustomerTaxId));
    }

    [Fact]
    public async Task Refund_issues_a_single_credit_note_for_the_delta_and_retries_after_failure()
    {
        var fx = Create();
        fx.Provider.FailuresLeft = 1;
        var order = await SeedOrderAsync(fx, TenantA, 40m, null, 23m);
        var payment = await SeedPaymentAsync(fx, order);
        await fx.Invoices.EnsureInvoiceReceiptAsync(payment.Id);
        var failed = await fx.Db.SalesDocuments.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(SalesDocumentStatus.Failed, failed.Status);

        await fx.Invoices.RetryAsync(failed.Id);
        payment.RefundedAmount = 10m;
        payment.Status = PaymentStatus.PartiallyRefunded;
        await fx.Db.SaveChangesAsync();
        await fx.Invoices.EnsureCreditNoteAsync(payment.Id);
        await fx.Invoices.EnsureCreditNoteAsync(payment.Id);
        payment.RefundedAmount = 40m;
        payment.Status = PaymentStatus.Refunded;
        await fx.Db.SaveChangesAsync();
        await fx.Invoices.EnsureCreditNoteAsync(payment.Id);

        var documents = await fx.Db.SalesDocuments.IgnoreQueryFilters().OrderBy(d => d.CreatedAt).ToListAsync();
        Assert.Equal(3, documents.Count);
        Assert.All(documents, d => Assert.Equal(SalesDocumentStatus.Issued, d.Status));
        Assert.Equal(10m, documents[1].GrossAmount);
        Assert.Equal(30m, documents[2].GrossAmount);
        Assert.Equal(SalesDocumentKind.CreditNote, documents[1].Kind);
    }

    [Fact]
    public async Task Documents_and_pdf_stay_inside_the_tenant()
    {
        var fx = Create();
        var orderA = await SeedOrderAsync(fx, TenantA, 12.30m, null, 23m);
        var paymentA = await SeedPaymentAsync(fx, orderA);
        var orderB = await SeedOrderAsync(fx, TenantB, 12.30m, null, 23m);
        var paymentB = await SeedPaymentAsync(fx, orderB);
        await fx.Invoices.EnsureInvoiceReceiptAsync(paymentA.Id);
        await fx.Invoices.EnsureInvoiceReceiptAsync(paymentB.Id);

        fx.Tenant.SetTenant(TenantA, "loja-a");
        var visible = await fx.Invoices.ListForFloristAsync();
        Assert.Single(visible);
        Assert.Equal(orderA.Id, visible[0].OrderId);

        var hidden = await fx.Db.SalesDocuments.IgnoreQueryFilters().SingleAsync(d => d.TenantId == TenantB);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fx.Invoices.OpenPdfAsync(hidden.Id, orderA.CustomerId, "Florista"));
        var own = await fx.Db.SalesDocuments.IgnoreQueryFilters().SingleAsync(d => d.TenantId == TenantA);
        var file = await fx.Invoices.OpenPdfAsync(own.Id, orderA.CustomerId, "Florista");
        Assert.NotEmpty(file.Content);

        fx.Tenant.SetTenant(TenantA, "loja-a");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            fx.Invoices.OpenPdfAsync(own.Id, Guid.NewGuid(), "Cliente"));
        var customerFile = await fx.Invoices.OpenPdfAsync(own.Id, orderA.CustomerId, "Cliente");
        Assert.NotEmpty(customerFile.Content);
    }

    [Fact]
    public async Task InvoiceXpress_uses_the_account_host_and_finalizes_the_receipt()
    {
        var handler = new ScriptedHandler();
        var provider = new InvoiceXpressInvoicingProvider(new HttpClient(handler));
        var call = new InvoicingCall(
            "receipt:abc",
            SalesDocumentKind.InvoiceReceipt,
            null,
            "123",
            null,
            new SellerProfile("Florista Legal", "500000000", "Rua A", "Lisboa", "1000-001", "Portugal"),
            new BuyerProfile("Ana", "ana@example.com", "123456789", "123456789"),
            [new InvoiceLine("Rosa", null, 1, 12.30m, 23m)]);

        var issued = await provider.IssueAsync(new ProviderSession("Loja-A", "secret-key", SalesDocumentKind.InvoiceReceipt), call, CancellationToken.None);

        Assert.Equal("42", issued.ProviderDocumentId);
        Assert.Equal("FR A/1", issued.DocumentNumber);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("https://loja-a.app.invoicexpress.com/invoice_receipts.json?api_key=secret-key", handler.Requests[0].Uri);
        Assert.Contains("IVA23", handler.Requests[0].Body);
        Assert.Contains("\"unit_price\":10", handler.Requests[0].Body);
        Assert.DoesNotContain("secret-key", handler.Requests[0].Body);
        Assert.EndsWith("/invoice_receipts/42/change-state.json?api_key=secret-key", handler.Requests[1].Uri);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.IssueAsync(new ProviderSession("../evil", "secret-key", SalesDocumentKind.InvoiceReceipt), call, CancellationToken.None));
    }

    private static UpdateInvoicingSettingsRequest Settings(string? apiKey = null, string? taxId = null) =>
        new(
            "Florista Legal, Lda.",
            taxId ?? "500000000",
            "Rua das Flores 1",
            "Lisboa",
            "1000-001",
            "Portugal",
            InvoicingProviderKind.Fake,
            null,
            apiKey,
            null,
            23m,
            null,
            true);

    private static async Task<Order> SeedOrderAsync(Fixture fx, Guid tenantId, decimal total, string? nif, decimal vat)
    {
        var customerId = Guid.NewGuid();
        var customer = new ApplicationUser
        {
            Id = customerId,
            Email = "ana@loja.pt",
            UserName = $"ana-{customerId:N}@loja.pt",
            FullName = "Ana Costa",
            TenantId = tenantId
        };
        fx.Db.Users.Add(customer);
        var order = new Order
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Customer = customer,
            Status = OrderStatus.Confirmed,
            EstimatedTotal = total,
            DeliveryFee = 0,
            CustomerTaxId = nif,
            DeliveryType = DeliveryType.StorePickup,
            BaseType = ArrangementBaseType.Sponge
        };
        order.Items.Add(new OrderItem
        {
            TenantId = tenantId,
            Name = "Rosa",
            Quantity = 1,
            UnitPrice = total,
            VatRate = vat
        });
        fx.Db.Orders.Add(order);
        await fx.Db.SaveChangesAsync();
        return order;
    }

    private static async Task<Payment> SeedPaymentAsync(Fixture fx, Order order)
    {
        var payment = new Payment
        {
            TenantId = order.TenantId,
            OrderId = order.Id,
            Status = PaymentStatus.Succeeded,
            Amount = order.EstimatedTotal,
            Currency = "eur"
        };
        fx.Db.Payments.Add(payment);
        await fx.Db.SaveChangesAsync();
        return payment;
    }

    private static Fixture Create()
    {
        var tenant = new TenantContext();
        tenant.SetTenant(TenantA, "loja-a");
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        db.Tenants.AddRange(
            new Tenant { Id = TenantA, Name = "Loja A", Slug = "loja-a" },
            new Tenant { Id = TenantB, Name = "Loja B", Slug = "loja-b" });
        db.SaveChanges();
        var uow = new UnitOfWork(db);
        var provider = new FlakyProvider();
        var files = new MemoryFiles();
        var invoices = new InvoiceService(
            uow,
            tenant,
            new SingleFactory(provider),
            new PrefixProtector(),
            files,
            new AuditLogger(uow, NullLogger<AuditLogger>.Instance),
            Options.Create(new InvoicingOptions { AllowFakeProvider = true }),
            NullLogger<InvoiceService>.Instance);
        db.FloristInvoicingSettings.Add(new FloristInvoicingSettings
        {
            TenantId = TenantA,
            LegalName = "Florista A",
            TaxId = "500000000",
            AddressLine = "Rua A",
            City = "Lisboa",
            PostalCode = "1000-001",
            Provider = InvoicingProviderKind.Fake,
            DefaultVatRate = 23m,
            Enabled = true
        });
        db.FloristInvoicingSettings.Add(new FloristInvoicingSettings
        {
            TenantId = TenantB,
            LegalName = "Florista B",
            TaxId = "500000000",
            AddressLine = "Rua B",
            City = "Porto",
            PostalCode = "4000-001",
            Provider = InvoicingProviderKind.Fake,
            DefaultVatRate = 23m,
            Enabled = true
        });
        db.SaveChanges();
        return new Fixture(db, tenant, invoices, provider, files);
    }

    private sealed record Fixture(
        ApplicationDbContext Db,
        TenantContext Tenant,
        InvoiceService Invoices,
        FlakyProvider Provider,
        MemoryFiles Files);

    private sealed class SingleFactory(IInvoicingProvider provider) : IInvoicingProviderFactory
    {
        public IInvoicingProvider Get(InvoicingProviderKind kind) => provider;
    }

    private sealed class PrefixProtector : ISecretProtector
    {
        public string Protect(string plaintext) => "enc:" + plaintext;
        public string Unprotect(string ciphertext) => ciphertext["enc:".Length..];
    }

    private sealed class FlakyProvider : IInvoicingProvider
    {
        private readonly FakeInvoicingProvider _inner = new();
        public int FailuresLeft { get; set; }
        public int IssueCalls { get; private set; }
        public InvoicingProviderKind Kind => InvoicingProviderKind.Fake;

        public Task<ProviderIssuedDocument> IssueAsync(ProviderSession session, InvoicingCall call, CancellationToken ct)
        {
            IssueCalls++;
            if (FailuresLeft > 0)
            {
                FailuresLeft--;
                throw new InvalidOperationException("fornecedor indisponível");
            }

            return _inner.IssueAsync(session, call, ct);
        }

        public Task<byte[]> DownloadPdfAsync(ProviderSession session, string providerDocumentId, CancellationToken ct) =>
            _inner.DownloadPdfAsync(session, providerDocumentId, ct);
    }

    private sealed class MemoryFiles : IFileStorage
    {
        private readonly Dictionary<string, byte[]> _files = new();

        public async Task<StoredFileDescriptor> SaveAsync(Stream content, string contentType, string originalFileName, Guid tenantId, CancellationToken ct = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            var path = $"{tenantId:N}/{Guid.NewGuid():N}.pdf";
            _files[path] = buffer.ToArray();
            return new StoredFileDescriptor(path, "/api/files/" + path, contentType, buffer.Length);
        }

        public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken ct = default) =>
            Task.FromResult<Stream?>(_files.TryGetValue(storagePath, out var bytes) ? new MemoryStream(bytes) : null);

        public void DeleteIfExists(string storagePath) => _files.Remove(storagePath);
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        public List<(string Uri, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.RequestUri!.ToString(), body));
            var path = request.RequestUri!.AbsolutePath;
            var json = path.Contains("change-state", StringComparison.Ordinal)
                ? """{"invoice_receipt":{"id":42,"status":"settled"}}"""
                : """{"invoice_receipt":{"id":42,"status":"draft","inverted_sequence_number":"FR A/1","permalink":"https://www.app.invoicexpress.com/documents/42"}}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
