using System.Collections.Concurrent;
using System.Text;
using FloristHub.DAL.Enums;

namespace FloristHub.Services.Invoicing;

/// <summary>
/// Fornecedor em memória para desenvolvimento e testes. Os documentos não têm valor fiscal.
/// </summary>
public sealed class FakeInvoicingProvider : IInvoicingProvider
{
    private int _sequence;
    private readonly ConcurrentDictionary<string, byte[]> _pdfs = new();

    public InvoicingProviderKind Kind => InvoicingProviderKind.Fake;

    public Task<ProviderIssuedDocument> IssueAsync(ProviderSession session, InvoicingCall call, CancellationToken ct)
    {
        var number = Interlocked.Increment(ref _sequence);
        var prefix = call.Kind == SalesDocumentKind.CreditNote ? "NC TEST" : "FR TEST";
        var documentNumber = $"{prefix}/{number}";
        var id = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var text = string.Join(' ',
            call.Kind == SalesDocumentKind.CreditNote ? "Nota de credito" : "Fatura-recibo",
            documentNumber,
            call.Seller.LegalName,
            call.Seller.TaxId,
            call.Buyer.Name,
            call.Buyer.TaxId ?? "consumidor-final",
            call.IdempotencyKey);
        var pdf = SimplePdf.Build(text);
        _pdfs[id] = pdf;
        return Task.FromResult(new ProviderIssuedDocument(id, documentNumber, $"https://fake.invoicing.local/{id}", pdf));
    }

    public Task<byte[]> DownloadPdfAsync(ProviderSession session, string providerDocumentId, CancellationToken ct)
    {
        if (!_pdfs.TryGetValue(providerDocumentId, out var pdf))
            throw new InvalidOperationException("PDF de teste não encontrado.");
        return Task.FromResult(pdf);
    }
}

internal static class SimplePdf
{
    public static byte[] Build(string text)
    {
        var safe = new string(text.Select(c => c is >= ' ' and <= '~' ? c : ' ').ToArray())
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
        if (safe.Length > 400)
            safe = safe[..400];

        var stream = $"BT /F1 11 Tf 40 780 Td ({safe}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Count 1 /Kids [3 0 R] >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(builder.Length);
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xref = builder.Length;
        builder.Append("xref\n0 ").Append(objects.Length + 1).Append('\n');
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
            builder.Append(offset.ToString("D10", System.Globalization.CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        builder.Append("trailer << /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R >>\nstartxref\n");
        builder.Append(xref).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }
}
