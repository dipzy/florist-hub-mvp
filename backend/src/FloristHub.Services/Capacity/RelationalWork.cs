using System.Collections.Concurrent;
using FloristHub.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Capacity;

public sealed class CapacityGate
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    public SemaphoreSlim For(Guid tenantId, DateOnly date) =>
        _gates.GetOrAdd($"{tenantId:N}:{date:yyyy-MM-dd}", _ => new SemaphoreSlim(1, 1));
}

public sealed class StockGate
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();

    public SemaphoreSlim For(Guid flowerId) =>
        _gates.GetOrAdd(flowerId, _ => new SemaphoreSlim(1, 1));
}

public interface IRelationalWork
{
    Task RunAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}

public sealed class RelationalWork : IRelationalWork
{
    private readonly ApplicationDbContext _db;

    public RelationalWork(ApplicationDbContext db) => _db = db;

    public async Task RunAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
        {
            await action(cancellationToken);
            return;
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        var attempt = 0;
        await strategy.ExecuteAsync(async () =>
        {
            attempt++;
            if (attempt > 1)
                await ResetAfterRollbackAsync(cancellationToken);

            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });
    }

    private async Task ResetAfterRollbackAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in _db.ChangeTracker.Entries().ToList())
        {
            if (entry.State == EntityState.Added)
                entry.State = EntityState.Detached;
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
                await entry.ReloadAsync(cancellationToken);
        }
    }
}
