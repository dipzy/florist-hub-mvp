using FloristHub.DAL.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FloristHub.Services.Push;

public sealed class PushBackgroundService : BackgroundService
{
    private readonly InMemoryPushQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PushBackgroundService> _logger;

    public PushBackgroundService(
        InMemoryPushQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PushBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var delivery = scope.ServiceProvider.GetRequiredService<PushDeliveryService>();
                await delivery.DeliverAsync(item, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha ao processar push {Type} da encomenda {OrderId}", item.Type, item.OrderId);
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var audit = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
                    await audit.LogAsync("PushDeliveryFailed", null, item.TenantId, "Push", item.OrderId, ex.Message, stoppingToken);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Não foi possível registar auditoria de falha de push");
                }
            }
        }
    }
}
