using FloristHub.DAL.Audit;
using FloristHub.Services.Interfaces.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Email;

public sealed class EmailBackgroundService : BackgroundService
{
    private readonly InMemoryEmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(
        InMemoryEmailQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<EmailOptions> options,
        ILogger<EmailBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var queued in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await DispatchWithRetriesAsync(queued, stoppingToken);
        }
    }

    private async Task DispatchWithRetriesAsync(QueuedEmail queued, CancellationToken stoppingToken)
    {
        var attempts = Math.Max(1, _options.MaxSendAttempts);
        Exception? lastError = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await sender.SendAsync(new EmailMessage(queued.To, queued.Subject, queued.HtmlBody, queued.ReplyTo), stoppingToken);
                return;
            }
            catch (Exception ex) when (attempt < attempts)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Envio de email falhou (tentativa {Attempt}/{Max})", attempt, attempts);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), stoppingToken);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        _logger.LogError(lastError, "Envio de email esgotou tentativas To={To} Subject={Subject}", RedactAddress(queued.To), queued.Subject);
        await AuditFailureAsync(queued, lastError, stoppingToken);
    }

    private async Task AuditFailureAsync(QueuedEmail queued, Exception? error, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var audit = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
            var details = $"To={RedactAddress(queued.To)}; Subject={queued.Subject}; Context={queued.AuditContext}";
            await audit.LogAsync("EmailDeliveryFailed", null, null, "Email", null, details, ct);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(auditEx, "Não foi possível registar auditoria de falha de email");
        }
    }

    internal static string RedactAddress(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "[redacted]";
        var at = email.IndexOf('@');
        return at <= 0 ? "[redacted]" : "***" + email[at..];
    }
}
