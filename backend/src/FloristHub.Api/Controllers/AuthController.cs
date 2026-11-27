using FloristHub.Api.Extensions;
using FloristHub.Services.DTOs.Auth;
using FloristHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FloristHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct) =>
        Ok(await _auth.RegisterAsync(request, ct));

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        Ok(await _auth.LoginAsync(request, ct));

    [Authorize]
    [HttpGet("contexts")]
    public async Task<ActionResult<IReadOnlyList<AuthContextOption>>> Contexts(CancellationToken ct) =>
        Ok(await _auth.GetAvailableContextsAsync(User.GetUserId(), ct));

    [Authorize]
    [HttpPost("switch-context")]
    public async Task<ActionResult<AuthResponse>> SwitchContext([FromBody] SwitchContextRequest request, CancellationToken ct) =>
        Ok(await _auth.SwitchContextAsync(User.GetUserId(), request, ct));

    /// <summary>Conclui o convite da florista definindo a palavra-passe (token de reset Identity, 72h).</summary>
    [AllowAnonymous]
    [HttpPost("florist-invite/complete")]
    public async Task<IActionResult> CompleteFloristInvite([FromBody] CompleteFloristInviteRequest request, CancellationToken ct)
    {
        await _auth.CompleteFloristInviteAsync(request, ct);
        return NoContent();
    }
}
