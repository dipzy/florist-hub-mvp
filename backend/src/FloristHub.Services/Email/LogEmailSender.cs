using FloristHub.Services.Interfaces.Email;
using Microsoft.Extensions.Logging;

namespace FloristHub.Services.Email;

public sealed class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger) => _logger = logger;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "EMAIL (dev) To={To} Subject={Subject} HtmlLength={Length}",
            message.To,
            message.Subject,
            message.HtmlBody.Length);
        return Task.CompletedTask;
    }
}
