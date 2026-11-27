using System.Net;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Legal;
using FloristHub.DAL.Seed;

namespace FloristHub.Api.IntegrationTests;

[Collection(PostgresApiCollection.Name)]
public sealed class GdprComplianceTests
{
    private readonly FloristHubPostgresFixture _fixture;

    public GdprComplianceTests(FloristHubPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Public_legal_versions_are_available_per_tenant()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTenantHeaders(ApiTestClient.Demo.TenantLina, DevelopmentDataSeeder.DemoTenantSlug);

        var versions = await client.ReadJsonAsync<LegalVersionsDto>(await client.GetAsync("/api/legal/versions"));
        Assert.NotNull(versions);
        Assert.Equal(LegalDocumentSeedContent.SeedVersion, versions!.TermsVersion);

        var terms = await client.ReadJsonAsync<PublicLegalDocumentDto>(await client.GetAsync("/api/legal/terms"));
        Assert.NotNull(terms);
        Assert.Contains("revisto por advogado", terms!.ContentMarkdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Customer_can_export_data_scoped_to_tenant()
    {
        var client = new ApiTestClient(_fixture.Factory.CreateClient());
        client.UseTestPrincipal(Roles.Customer, ApiTestClient.Demo.CustomerUser, ApiTestClient.Demo.TenantLina, ApiTestClient.Demo.CustomerEmail);
        client.UseTenantHeaders(ApiTestClient.Demo.TenantLina, DevelopmentDataSeeder.DemoTenantSlug);

        var export = await client.ReadJsonAsync<CustomerExportDto>(await client.GetAsync("/api/privacy/export"));
        Assert.NotNull(export);
        Assert.Equal(ApiTestClient.Demo.CustomerUser, export!.UserId);
    }

    private sealed record LegalVersionsDto(string TermsVersion, string PrivacyVersion, string CookiesVersion);

    private sealed record PublicLegalDocumentDto(int DocumentType, string Version, string Title, string ContentMarkdown, string? ResponsibleSectionMarkdown);

    private sealed record CustomerExportDto(Guid UserId, string? Email, IReadOnlyList<object> Orders, DateTimeOffset ExportedAt);
}
