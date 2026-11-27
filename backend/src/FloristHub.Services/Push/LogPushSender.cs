using Microsoft.Extensions.Logging;

namespace FloristHub.Services.Push;

public sealed class LogPushSender : IPushSender
{
    private readonly ILogger<LogPushSender> _logger;

    public LogPushSender(ILogger<LogPushSender> logger) => _logger = logger;

    public Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "PUSH (dev) Type={Type} Order={OrderId} Tenant={TenantId} Title={Title}",
            message.Type,
            message.OrderId,
            message.TenantId,
            message.Title);
        return Task.FromResult(new PushSendResult(PushDeliveryOutcome.Sent));
    }
}
