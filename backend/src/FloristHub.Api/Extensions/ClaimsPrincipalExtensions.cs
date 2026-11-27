using System.Security.Claims;
using FloristHub.DAL.Constants;

namespace FloristHub.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    user.FindFirstValue(ClaimTypes.Name) ??
                    user.FindFirstValue(ClaimTypes.Actor) ??
                    user.FindFirst("sub")?.Value;

        if (value is null || !Guid.TryParse(value, out var id))
            throw new UnauthorizedAccessException("Utilizador não autenticado.");

        return id;
    }

    public static Guid GetTenantId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(CustomClaimTypes.TenantId);
        return value is not null && Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}
