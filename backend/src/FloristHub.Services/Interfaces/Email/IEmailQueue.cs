namespace FloristHub.Services.Interfaces.Email;

public interface IEmailQueue
{
    ValueTask EnqueueAsync(QueuedEmail message, CancellationToken cancellationToken = default);
}

public sealed record QueuedEmail(
    string To,
    string Subject,
    string HtmlBody,
    string? ReplyTo = null,
    string? AuditContext = null);
