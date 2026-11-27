using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FloristHub.Services;
using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FloristHub.Api.Auth;

public class JwtTokenService : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public (string Token, DateTime Expires) GenerateToken(ApplicationUser user, string role, Tenant? tenant)
    {
        var jwt = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var expires = DateTime.UtcNow.AddHours(double.Parse(jwt["ExpiresHours"] ?? "8"));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, role)
        };

        if (tenant is not null)
        {
            claims.Add(new Claim(CustomClaimTypes.TenantId, tenant.Id.ToString()));
            claims.Add(new Claim(CustomClaimTypes.TenantSlug, tenant.Slug));
        }

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
