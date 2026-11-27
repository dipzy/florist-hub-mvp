using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using FloristHub.DAL.Enums;

namespace FloristHub.Services.Invoicing;

public sealed class InvoiceXpressInvoicingProvider : IInvoicingProvider
{
    public const string HostSuffix = ".app.invoicexpress.com";
    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null
    };

    private readonly HttpClient _http;

    public InvoiceXpressInvoicingProvider(HttpClient http) => _http = http;

    public InvoicingProviderKind Kind => InvoicingProviderKind.InvoiceXpress;

    public async Task<ProviderIssuedDocument> IssueAsync(ProviderSession session, InvoicingCall call, CancellationToken ct)
    {
        var account = RequireAccount(session.AccountName);
        var apiKey = RequireKey(session.ApiKey);
        var collection = Collection(call.Kind);
        var created = await SendAsync(account, apiKey, HttpMethod.Post, $"{collection}.json", CreatePayload(call), ct);
        var node = DocumentNode(created);
        var id = ReadId(node);
        var status = ReadString(node, "status") ?? ReadString(node, "state");
        if (!IsFinal(status))
        {
            try
            {
                await SendAsync(
                    account,
                    apiKey,
                    HttpMethod.Put,
                    $"{collection}/{id}/change-state.json",
                    new { invoice = new { state = "finalized" } },
                    ct);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvoicingPartialException(id, ex.Message);
            }
        }

        var number = ReadString(node, "inverted_sequence_number")
            ?? ReadString(node, "sequence_number")
            ?? id;
        return new ProviderIssuedDocument(id, number, ReadString(node, "permalink"), null);
    }

    public async Task<byte[]> DownloadPdfAsync(ProviderSession session, string providerDocumentId, CancellationToken ct)
    {
        var account = RequireAccount(session.AccountName);
        var apiKey = RequireKey(session.ApiKey);
        var collection = Collection(session.Kind);
        try
        {
            await SendAsync(
                account,
                apiKey,
                HttpMethod.Put,
                $"{collection}/{Uri.EscapeDataString(providerDocumentId)}/change-state.json",
                new { invoice = new { state = "finalized" } },
                ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("422", StringComparison.Ordinal) || ex.Message.Contains("409", StringComparison.Ordinal))
        {
            // Já finalizado.
        }

        var meta = await SendAsync(
            account,
            apiKey,
            HttpMethod.Get,
            $"api/pdf/{Uri.EscapeDataString(providerDocumentId)}.json",
            null,
            ct);
        var url = ReadPdfUrl(meta) ?? throw new InvalidOperationException("O InvoiceXpress não devolveu o PDF.");
        return await DownloadHttpsAsync(url, apiKey, ct);
    }

    internal static string RequireAccount(string? accountName)
    {
        var account = accountName?.Trim().ToLowerInvariant() ?? "";
        if (account.Length is < 2 or > 63 || !account.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') || account.StartsWith('-') || account.EndsWith('-'))
            throw new InvalidOperationException("Nome da conta InvoiceXpress inválido.");
        return account;
    }

    private static string RequireKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Chave da API InvoiceXpress em falta.");
        return apiKey.Trim();
    }

    private static string Collection(SalesDocumentKind kind) => kind switch
    {
        SalesDocumentKind.CreditNote => "credit_notes",
        _ => "invoice_receipts"
    };

    private static object CreatePayload(InvoicingCall call)
    {
        var today = DateTime.UtcNow.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        int? owner = null;
        if (call.Kind == SalesDocumentKind.CreditNote && int.TryParse(call.OwnerProviderDocumentId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ownerId))
            owner = ownerId;

        return new
        {
            invoice = new
            {
                date = today,
                due_date = today,
                reference = Truncate(call.IdempotencyKey, 100),
                observations = Truncate(call.Kind == SalesDocumentKind.CreditNote
                    ? "Nota de crédito Florist Hub"
                    : "Fatura-recibo Florist Hub", 400),
                sequence_id = string.IsNullOrWhiteSpace(call.SequenceId) ? null : call.SequenceId.Trim(),
                tax_exemption = string.IsNullOrWhiteSpace(call.TaxExemptionCode) ? null : call.TaxExemptionCode.Trim(),
                owner_invoice_id = owner,
                client = new
                {
                    name = Truncate(call.Buyer.Name, 200),
                    code = Truncate(call.Buyer.Code, 50),
                    email = call.Buyer.Email,
                    fiscal_id = call.Buyer.TaxId,
                    country = "Portugal"
                },
                items = call.Lines.Select(line => new
                {
                    name = Truncate(line.Name, 200),
                    description = string.IsNullOrWhiteSpace(line.Description) ? null : Truncate(line.Description, 400),
                    unit_price = line.NetUnitPrice,
                    quantity = line.Quantity,
                    tax = new { name = VatRates.InvoiceXpressName(line.VatRate) }
                }).ToList()
            }
        };
    }

    private async Task<JsonElement> SendAsync(string account, string apiKey, HttpMethod method, string relativePath, object? body, CancellationToken ct)
    {
        var uri = new Uri($"https://{account}{HostSuffix}/{relativePath}?api_key={Uri.EscapeDataString(apiKey)}");
        using var request = new HttpRequestMessage(method, uri);
        if (body is not null)
            request.Content = JsonContent(body);
        try
        {
            using var response = await _http.SendAsync(request, ct);
            var text = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                if (text.Length > 400)
                    text = text[..400];
                throw new InvalidOperationException($"InvoiceXpress respondeu {(int)response.StatusCode} em {relativePath}: {Redact(text, apiKey)}");
            }

            if (string.IsNullOrWhiteSpace(text))
                return default;
            using var document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(Redact(ex.Message, apiKey), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(Redact(ex.Message, apiKey), ex);
        }
    }

    private async Task<byte[]> DownloadHttpsAsync(string url, string apiKey, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !await IsPublicHttpsAsync(uri, ct))
            throw new InvalidOperationException("URL do PDF recusado.");

        try
        {
            return await _http.GetByteArrayAsync(uri, ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(Redact(ex.Message, apiKey), ex);
        }
    }

    internal static string Redact(string? text, string? secret)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        if (string.IsNullOrEmpty(secret))
            return text;
        return text.Replace(secret, "[redacted]", StringComparison.Ordinal);
    }

    internal static async Task<bool> IsPublicHttpsAsync(Uri uri, CancellationToken ct)
    {
        if (uri.Scheme != Uri.UriSchemeHttps)
            return false;
        return await IsPublicHostAsync(uri.Host, ct);
    }

    internal static async Task<bool> IsPublicHostAsync(string host, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(host)
            || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
            return false;
        if (IPAddress.TryParse(host, out var literal))
            return !IsPrivate(literal);

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host, ct);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(ip => !IsPrivate(ip));
    }

    private static bool IsPrivate(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;
        if (ip.AddressFamily != AddressFamily.InterNetwork)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
                return true;
            var raw = ip.GetAddressBytes();
            return raw.Length > 0 && (raw[0] & 0xfe) == 0xfc;
        }
        var bytes = ip.GetAddressBytes();
        return bytes[0] is 10 or 127
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            || (bytes[0] == 169 && bytes[1] == 254);
    }

    private static JsonElement DocumentNode(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Resposta InvoiceXpress inválida.");
        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object && property.Value.TryGetProperty("id", out _))
                return property.Value;
        }

        throw new InvalidOperationException("Resposta InvoiceXpress sem documento.");
    }

    private static string? ReadPdfUrl(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("output", out var output))
        {
            var url = ReadString(output, "pdfUrl") ?? ReadString(output, "pdf_url");
            if (!string.IsNullOrWhiteSpace(url))
                return url;
        }

        return ReadString(root, "pdfUrl") ?? ReadString(root, "pdf_url");
    }

    private static string ReadId(JsonElement node)
    {
        var id = node.GetProperty("id");
        return id.ValueKind == JsonValueKind.Number
            ? id.GetInt64().ToString(CultureInfo.InvariantCulture)
            : id.GetString() ?? throw new InvalidOperationException("Documento InvoiceXpress sem identificador.");
    }

    private static string? ReadString(JsonElement node, string name)
    {
        if (!node.TryGetProperty(name, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static bool IsFinal(string? status) =>
        status is not null && status.Equals("draft", StringComparison.OrdinalIgnoreCase) == false && status.Length > 0;

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];

    private static StringContent JsonContent(object body) =>
        new(JsonSerializer.Serialize(body, Json), System.Text.Encoding.UTF8, "application/json");
}

public sealed class InvoicingPartialException : Exception
{
    public InvoicingPartialException(string providerDocumentId, string message) : base(message) =>
        ProviderDocumentId = providerDocumentId;

    public string ProviderDocumentId { get; }
}
