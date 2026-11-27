using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FloristHub.Api.Middleware;
using FloristHub.DAL.Seed;

namespace FloristHub.Api.IntegrationTests;

public sealed class ApiTestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private string? _token;

    public ApiTestClient(HttpClient http) => _http = http;

    public void ClearAuth()
    {
        _token = null;
        _http.DefaultRequestHeaders.Authorization = null;
        _http.DefaultRequestHeaders.Remove(TenantResolutionMiddleware.TenantIdHeader);
        _http.DefaultRequestHeaders.Remove(TenantResolutionMiddleware.TenantSlugHeader);
    }

    public void UseTestPrincipal(string role, Guid userId, Guid tenantId, string email = "test@local")
    {
        _token = $"{role};{userId};{tenantId};{email}";
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            IntegrationTestAuthHandler.Scheme,
            _token);
    }

    public void UseTenantHeaders(Guid tenantId, string? slug = null)
    {
        _http.DefaultRequestHeaders.Remove(TenantResolutionMiddleware.TenantIdHeader);
        _http.DefaultRequestHeaders.Add(TenantResolutionMiddleware.TenantIdHeader, tenantId.ToString());
        if (!string.IsNullOrWhiteSpace(slug))
        {
            _http.DefaultRequestHeaders.Remove(TenantResolutionMiddleware.TenantSlugHeader);
            _http.DefaultRequestHeaders.Add(TenantResolutionMiddleware.TenantSlugHeader, slug);
        }
    }

    public async Task<string> LoginAsync(string email, string password, Guid? tenantId = null)
    {
        var auth = await LoginAuthAsync(email, password, tenantId);
        return auth.Token;
    }

    public async Task<LoginAuthBody> LoginAuthAsync(string email, string password, Guid? tenantId = null)
    {
        var body = new { email, password, tenantId };
        using var res = await _http.PostAsJsonAsync("/api/auth/login", body);
        res.EnsureSuccessStatusCode();
        var auth = await ReadJsonAsync<LoginAuthBody>(res);
        return auth ?? throw new InvalidOperationException("Resposta de login inválida.");
    }

    public void UseBearerToken(string token)
    {
        _token = token;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public Task<HttpResponseMessage> GetAsync(string path) => SendAsync(HttpMethod.Get, path);

    public Task<HttpResponseMessage> SendJsonAsync(HttpMethod method, string path, object? body = null) =>
        SendAsync(method, path, body);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null)
    {
        var req = new HttpRequestMessage(method, path);
        if (!string.IsNullOrEmpty(_token))
        {
            var scheme = _token.Contains(';', StringComparison.Ordinal)
                ? IntegrationTestAuthHandler.Scheme
                : "Bearer";
            req.Headers.Authorization = new AuthenticationHeaderValue(scheme, _token);
        }
        if (body is not null)
            req.Content = JsonContent.Create(body);
        return await _http.SendAsync(req);
    }

    public async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentLength == 0)
            return default;
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
    }

    public static class Demo
    {
        public static readonly Guid TenantLina = Guid.Parse("11111111-1111-1111-1111-111111111101");
        public static readonly Guid TenantCampo = Guid.Parse("11111111-1111-1111-1111-111111111102");
        public static readonly Guid RoseLina = Guid.Parse("33333333-3333-3333-3333-333333333301");
        public static readonly Guid FloristCampoUser = Guid.Parse("22222222-2222-2222-2222-222222222204");
        public static readonly Guid FloristLinaUser = Guid.Parse("22222222-2222-2222-2222-222222222202");
        public static readonly Guid CustomerUser = Guid.Parse("22222222-2222-2222-2222-222222222203");
        public const string FloristLinaEmail = "florista@demo.floristhub.pt";
        public const string FloristCampoEmail = "florista.campo@demo.floristhub.pt";
        public const string CustomerEmail = "cliente@demo.floristhub.pt";
        public static readonly string Password = DevelopmentDataSeeder.DemoPassword;
        public static readonly string SuperAdminEmail = DevelopmentDataSeeder.SuperAdminEmail;
    }

    public sealed record LoginAuthBody(
        string Token,
        string Role,
        Guid? TenantId,
        IReadOnlyList<AuthContextBody>? AvailableContexts);

    public sealed record AuthContextBody(string Kind, Guid? TenantId, string? TenantName, string? TenantSlug);

    public sealed record CreateTenantBody(
        PlatformTenantDetailBody Tenant,
        string? DevInviteLink,
        bool FloristInviteSent);

    public sealed record PlatformTenantDetailBody(Guid Id, string Name, string Slug);
}
