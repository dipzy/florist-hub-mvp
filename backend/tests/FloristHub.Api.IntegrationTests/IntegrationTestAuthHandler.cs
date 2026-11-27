using System.Security.Claims;
using System.Text.Encodings.Web;
using FloristHub.DAL.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FloristHub.Api.IntegrationTests;

/// <summary>Autenticação determinística para testes de integração (substitui JWT no factory).</summary>
public sealed class IntegrationTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public new const string Scheme = "IntegrationTest";
    public const string HeaderPrefix = "IntegrationTest ";

    public IntegrationTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var payload = header[HeaderPrefix.Length..];
        var parts = payload.Split(';', 4);
        if (parts.Length < 3)
            return Task.FromResult(AuthenticateResult.Fail("Cabeçalho de teste inválido."));

        var role = parts[0];
        if (!Guid.TryParse(parts[1], out var userId) || !Guid.TryParse(parts[2], out var tenantId))
            return Task.FromResult(AuthenticateResult.Fail("IDs inválidos no cabeçalho de teste."));

        var email = parts.Length > 3 ? parts[3] : "test@local";

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new(CustomClaimTypes.TenantId, tenantId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
        };

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme)));
    }
}
