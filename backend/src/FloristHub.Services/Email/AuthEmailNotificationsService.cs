using FloristHub.DAL.Entities;
using FloristHub.Services.Email.Templates;
using FloristHub.Services.Interfaces.Email;

namespace FloristHub.Services.Email;

public sealed class AuthEmailNotificationsService : IAuthEmailNotifications
{
    private readonly IEmailQueue _queue;

    public AuthEmailNotificationsService(IEmailQueue queue) => _queue = queue;

    public void WelcomeRegistered(ApplicationUser user, Tenant tenant, string role)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            return;

        var (subject, html) = TransactionalEmailTemplates.Welcome(tenant, user, role);
        _ = _queue.EnqueueAsync(new QueuedEmail(
            user.Email,
            subject,
            html,
            AuditContext: $"Welcome:{user.Id}"));
    }
}
