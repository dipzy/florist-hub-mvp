using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FloristHub.Services.Interfaces.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Email;

public sealed class ResendEmailSender : IEmailSender
{
    public const string HttpClientName = "ResendEmailApi";

    private readonly HttpClient _http;
    private readonly EmailOptions _options;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient http, IOptions<EmailOptions> options, ILogger<ResendEmailSender> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ResendApiKey))
            throw new InvalidOperationException("Email:ResendApiKey em falta.");

        var payload = new ResendSendEmailRequest(
            _options.From,
            new[] { message.To },
            message.Subject,
            message.HtmlBody,
            message.ReplyTo);

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ResendApiKey);
        request.Content = JsonContent.Create(payload);

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Resend falhou Status={Status} Body={Body}", (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }

    internal sealed record ResendSendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] IReadOnlyList<string> To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html,
        [property: JsonPropertyName("reply_to")] string? ReplyTo);
}
