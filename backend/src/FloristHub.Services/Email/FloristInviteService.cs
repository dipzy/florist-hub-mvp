using FloristHub.DAL.Entities;
using FloristHub.Services.Interfaces.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Email;

public interface IFloristInviteService
{
    /// <summary>Envia convite por email. Em modo Log devolve o URL para o SuperAdmin (apenas desenvolvimento).</summary>
    Task<string?> SendInviteAsync(ApplicationUser florist, Tenant tenant, CancellationToken ct = default);
}

public sealed class FloristInviteService : IFloristInviteService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailQueue _queue;
    private readonly EmailOptions _options;
    private readonly ILogger<FloristInviteService> _logger;
    private readonly IFloristInviteStore _invites;

    public FloristInviteService(
        UserManager<ApplicationUser> users,
        IEmailQueue queue,
        IOptions<EmailOptions> options,
        ILogger<FloristInviteService> logger,
        IFloristInviteStore invites)
    {
        _users = users;
        _queue = queue;
        _options = options.Value;
        _logger = logger;
        _invites = invites;
    }

    public async Task<string?> SendInviteAsync(ApplicationUser florist, Tenant tenant, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(florist.Email))
            throw new InvalidOperationException("A florista não tem email.");

        await _users.UpdateSecurityStampAsync(florist);
        await _invites.IssueAsync(florist.Id, tenant.Id, ct);
        var token = await _users.GeneratePasswordResetTokenAsync(florist);
        var (subject, html, inviteUrl) = FloristInviteEmailComposer.Compose(
            tenant,
            florist,
            _options.WebBaseUrl,
            token);

        await _queue.EnqueueAsync(new QueuedEmail(
            florist.Email,
            subject,
            html,
            AuditContext: $"FloristInvite:{florist.Id}:{tenant.Id}"), ct);

        if (IsLogProvider())
        {
            _logger.LogInformation(
                "Convite de florista (modo Log) Loja={Store} Email={Email}",
                tenant.Slug,
                EmailBackgroundService.RedactAddress(florist.Email));
            return inviteUrl;
        }

        return null;
    }

    private bool IsLogProvider() =>
        string.IsNullOrWhiteSpace(_options.Provider)
        || _options.Provider.Equals("Log", StringComparison.OrdinalIgnoreCase);
}
