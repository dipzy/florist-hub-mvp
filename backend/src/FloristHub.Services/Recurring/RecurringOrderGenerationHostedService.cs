using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Recurring;

public sealed class RecurringOrderGenerationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RecurringOrderOptions _options;
    private readonly ILogger<RecurringOrderGenerationHostedService> _logger;

    public RecurringOrderGenerationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RecurringOrderOptions> options,
        ILogger<RecurringOrderGenerationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(15, _options.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var generation = scope.ServiceProvider.GetRequiredService<IRecurringOrderGenerationService>();
                var count = await generation.ProcessDueAsync(stoppingToken);
                if (count > 0)
                    _logger.LogInformation("Geradas {Count} encomendas recorrentes", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo de geração de recorrências");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}
