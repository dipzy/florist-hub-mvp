using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.Legal;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services;
using FloristHub.Services.DTOs.Legal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FloristHub.Services.Tests.Legal;

public class LegalComplianceServiceTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task GetDocument_includes_tenant_responsible_section()
    {
        var fx = Create();
        var doc = await fx.Service.GetDocumentAsync(LegalDocumentType.Privacy);
        Assert.Contains("Responsável pelo tratamento", doc.ResponsibleSectionMarkdown);
        Assert.Contains("123456789", doc.ResponsibleSectionMarkdown);
    }

    [Fact]
    public void EnsureCheckoutConsent_rejects_stale_versions()
    {
        var fx = Create();
        var current = new LegalVersionsDto(LegalDocumentSeedContent.SeedVersion, LegalDocumentSeedContent.SeedVersion, LegalDocumentSeedContent.SeedVersion);
        Assert.Throws<InvalidOperationException>(() =>
            fx.Service.EnsureCheckoutConsent(new LegalConsentRequest(true, "old", LegalDocumentSeedContent.SeedVersion), current));
    }

    [Fact]
    public async Task RecordOrderConsent_persists_versions_on_order()
    {
        var fx = Create();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            CustomerId = Guid.NewGuid(),
            BaseType = ArrangementBaseType.Sponge,
            DeliveryType = DeliveryType.StorePickup,
        };
        var consent = new LegalConsentRequest(true, LegalDocumentSeedContent.SeedVersion, LegalDocumentSeedContent.SeedVersion);
        await fx.Service.RecordOrderConsentAsync(order, consent, order.CustomerId);
        Assert.Equal(LegalDocumentSeedContent.SeedVersion, order.AcceptedTermsVersion);
        Assert.NotNull(order.LegalConsentRecordedAt);
    }

    private static Fixture Create()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(TenantA, "loja-a");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options, tenantContext);
        db.Tenants.Add(new Tenant
        {
            Id = TenantA,
            Name = "Loja A",
            Slug = "loja-a",
            AddressLine = "Rua das Flores 1",
            City = "Lisboa",
            ContactEmail = "florista@loja.pt",
        });
        db.FloristInvoicingSettings.Add(new FloristInvoicingSettings
        {
            TenantId = TenantA,
            LegalName = "Florista A Lda.",
            TaxId = "123456789",
            AddressLine = "Rua das Flores 1",
            City = "Lisboa",
            PostalCode = "1000-001",
            Country = "Portugal",
        });
        db.LegalDocumentVersions.AddRange(
            new LegalDocumentVersion
            {
                DocumentType = LegalDocumentType.Terms,
                Version = LegalDocumentSeedContent.SeedVersion,
                Title = "Termos",
                ContentMarkdown = LegalDocumentSeedContent.TermsBody(),
                IsCurrent = true,
            },
            new LegalDocumentVersion
            {
                DocumentType = LegalDocumentType.Privacy,
                Version = LegalDocumentSeedContent.SeedVersion,
                Title = "Privacidade",
                ContentMarkdown = LegalDocumentSeedContent.PrivacyBody(),
                IsCurrent = true,
            },
            new LegalDocumentVersion
            {
                DocumentType = LegalDocumentType.Cookies,
                Version = LegalDocumentSeedContent.SeedVersion,
                Title = "Cookies",
                ContentMarkdown = LegalDocumentSeedContent.CookiesBody(),
                IsCurrent = true,
            });
        db.SaveChanges();

        var uow = new UnitOfWork(db);
        var audit = new AuditLogger(uow, NullLogger<AuditLogger>.Instance);
        var service = new LegalComplianceService(uow, tenantContext, audit);
        return new Fixture(db, service);
    }

    private sealed record Fixture(ApplicationDbContext Db, LegalComplianceService Service);
}
