using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FloristHub.Services.Push;

public sealed class FcmPushSender : IPushSender
{
    public const string HttpClientName = "fcm";
    private const string MessagingScope = "https://www.googleapis.com/auth/firebase.messaging";

    private readonly HttpClient _http;
    private readonly PushOptions _options;
    private readonly ILogger<FcmPushSender> _logger;
    private readonly SemaphoreSlim _tokenGate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpires;

    public FcmPushSender(HttpClient http, IOptions<PushOptions> options, ILogger<FcmPushSender> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await GetAccessTokenAsync(cancellationToken);
            var projectId = _options.FcmProjectId!.Trim();
            var payload = JsonSerializer.Serialize(new
            {
                message = new
                {
                    token = message.DeviceToken,
                    notification = new { title = message.Title, body = message.Body },
                    data = BuildData(message),
                    android = new { priority = "HIGH" },
                    apns = new { payload = new { aps = new { sound = "default" } } }
                }
            }, JsonOptions);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(projectId)}/messages:send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var outcome = FcmErrorClassifier.Classify((int)response.StatusCode, body);
            if (outcome != PushDeliveryOutcome.Sent)
            {
                _logger.LogWarning(
                    "FCM recusou o envio Status={Status} Outcome={Outcome} Order={OrderId}",
                    (int)response.StatusCode,
                    outcome,
                    message.OrderId);
            }

            return new PushSendResult(outcome);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            _logger.LogWarning(ex, "FCM indisponível Order={OrderId}", message.OrderId);
            return new PushSendResult(PushDeliveryOutcome.Retry);
        }
    }

    private static Dictionary<string, string> BuildData(PushMessage message)
    {
        var data = new Dictionary<string, string>
        {
            ["tenantId"] = message.TenantId.ToString(),
            ["type"] = message.Type,
            ["title"] = message.Title,
            ["body"] = message.Body
        };
        if (message.OrderId != Guid.Empty)
            data["orderId"] = message.OrderId.ToString();
        return data;
    }

    internal static string CreateAssertion(string clientEmail, string privateKeyPem, DateTimeOffset now)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        var jwt = new JwtSecurityToken(
            issuer: clientEmail,
            audience: "https://oauth2.googleapis.com/token",
            claims: [new Claim("scope", MessagingScope)],
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(55).UtcDateTime,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is not null && _accessTokenExpires > DateTimeOffset.UtcNow.AddMinutes(1))
            return _accessToken;

        await _tokenGate.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && _accessTokenExpires > DateTimeOffset.UtcNow.AddMinutes(1))
                return _accessToken;

            var account = ReadServiceAccount();
            var assertion = CreateAssertion(account.Email, account.PrivateKey, DateTimeOffset.UtcNow);
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion
            });
            using var response = await _http.PostAsync("https://oauth2.googleapis.com/token", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Não foi possível obter token de acesso FCM.");

            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var seconds) ? seconds.GetInt32() : 3600;
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Resposta OAuth sem access_token.");

            _accessToken = token;
            _accessTokenExpires = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return token;
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    private (string Email, string PrivateKey) ReadServiceAccount()
    {
        var json = _options.FcmServiceAccountJson;
        if (string.IsNullOrWhiteSpace(json) && !string.IsNullOrWhiteSpace(_options.FcmServiceAccountPath))
            json = File.ReadAllText(_options.FcmServiceAccountPath);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Conta de serviço FCM em falta.");

        using var doc = JsonDocument.Parse(json);
        var email = doc.RootElement.GetProperty("client_email").GetString();
        var key = doc.RootElement.GetProperty("private_key").GetString();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Conta de serviço FCM incompleta.");
        return (email, key);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
