using FloristHub.Services.Invoicing;

namespace FloristHub.Services.Tests.Invoicing;

public class InvoiceXpressSafetyTests
{
    [Fact]
    public void Redact_removes_the_api_key_from_error_text()
    {
        var secret = "ix-live-secret-key";
        var message = InvoiceXpressInvoicingProvider.Redact(
            $"GET https://conta.app.invoicexpress.com/invoice_receipts.json?api_key={secret} falhou",
            secret);
        Assert.DoesNotContain(secret, message, StringComparison.Ordinal);
        Assert.Contains("[redacted]", message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://example.com/pdf")]
    [InlineData("https://127.0.0.1/latest")]
    [InlineData("https://169.254.169.254/latest")]
    [InlineData("https://10.1.2.3/secret")]
    [InlineData("https://localhost/pdf")]
    public async Task Pdf_url_rejects_private_or_non_https_hosts(string url)
    {
        var uri = new Uri(url);
        Assert.False(await InvoiceXpressInvoicingProvider.IsPublicHttpsAsync(uri, CancellationToken.None));
    }

}