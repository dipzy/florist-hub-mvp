using FloristHub.Services.Capacity;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Flowers;
using FloristHub.Services.Email.Templates;
using FloristHub.Services.Interfaces.Email;
using FloristHub.Services.Push;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Stock;

public interface IStockService
{
    Task WithFlowerLocksAsync(Order order, Func<CancellationToken, Task> action, CancellationToken ct = default);
    Task ApplyConfirmAsync(Order order, CancellationToken ct = default);
    Task ApplyRestoreAsync(Order order, StockMovementReason reason, CancellationToken ct = default);
    Task ApplyRestoreForOrderAsync(Guid orderId, StockMovementReason reason, CancellationToken ct = default);
    void RecordAdjustment(Flower flower, int? previousQuantity, int? previousMinimum);
    Task<IReadOnlyList<StockMovementDto>> ListMovementsAsync(Guid flowerId, CancellationToken ct = default);
    Task<IReadOnlyList<LowStockAlertDto>> ListLowStockAsync(CancellationToken ct = default);
    Task FlushNoticesAsync(CancellationToken ct = default);
}

public sealed class StockService : IStockService
{
    private readonly IUnitOfWork _uow;
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IEmailQueue _emails;
    private readonly IPushQueue _push;
    private readonly StockGate _gate;
    private readonly IRelationalWork? _work;
    private readonly List<LowStockNotice> _pending = [];

    public StockService(
        IUnitOfWork uow,
        ApplicationDbContext db,
        ITenantContext tenant,
        IEmailQueue emails,
        IPushQueue push,
        StockGate gate,
        IRelationalWork? work = null)
    {
        _uow = uow;
        _db = db;
        _tenant = tenant;
        _emails = emails;
        _push = push;
        _gate = gate;
        _work = work;
    }

    public async Task WithFlowerLocksAsync(Order order, Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        await EnsureItemsAsync(order, ct);
        var ids = order.Items.Where(i => i.FlowerId.HasValue).Select(i => i.FlowerId!.Value).Distinct().OrderBy(id => id).ToList();
        var held = new List<SemaphoreSlim>();
        try
        {
            foreach (var id in ids)
            {
                var gate = _gate.For(id);
                await gate.WaitAsync(ct);
                held.Add(gate);
            }

            if (_db.Database.CurrentTransaction is null && _db.Database.IsRelational() && _work is not null)
                await _work.RunAsync(async inner =>
                {
                    await LockFlowersAsync(ids, inner);
                    await action(inner);
                }, ct);
            else
            {
                await LockFlowersAsync(ids, ct);
                await action(ct);
            }
        }
        finally
        {
            for (var i = held.Count - 1; i >= 0; i--)
                held[i].Release();
        }
    }

    public async Task ApplyConfirmAsync(Order order, CancellationToken ct = default)
    {
        if (order.StockCommitted)
            return;

        await EnsureItemsAsync(order, ct);
        var lines = Lines(order);
        if (lines.Count == 0)
        {
            order.StockCommitted = true;
            return;
        }

        var flowers = await LoadFlowersAsync(lines.Select(l => l.Id), ct);
        foreach (var line in lines)
        {
            if (!flowers.TryGetValue(line.Id, out var flower) || flower.StockQuantity is null)
                continue;
            if (flower.StockQuantity.Value < line.Qty)
                throw new InvalidOperationException($"Stock insuficiente de {flower.Name}.");
        }

        foreach (var line in lines)
        {
            if (!flowers.TryGetValue(line.Id, out var flower) || flower.StockQuantity is null)
                continue;
            var previous = flower.StockQuantity.Value;
            flower.StockQuantity = previous - line.Qty;
            flower.UpdatedAt = DateTimeOffset.UtcNow;
            AddMovement(flower, order.Id, -line.Qty, flower.StockQuantity, StockMovementReason.OrderConfirmed, "Confirmação da encomenda.");
            QueueIfCrossed(flower, previous);
        }

        order.StockCommitted = true;
    }

    public async Task ApplyRestoreAsync(Order order, StockMovementReason reason, CancellationToken ct = default)
    {
        if (!order.StockCommitted)
            return;

        await EnsureItemsAsync(order, ct);
        var lines = Lines(order);
        var flowers = await LoadFlowersAsync(lines.Select(l => l.Id), ct);
        foreach (var line in lines)
        {
            if (!flowers.TryGetValue(line.Id, out var flower) || flower.StockQuantity is null)
                continue;
            var previous = flower.StockQuantity.Value;
            flower.StockQuantity = previous + line.Qty;
            flower.UpdatedAt = DateTimeOffset.UtcNow;
            var note = reason == StockMovementReason.OrderRefunded ? "Reembolso da encomenda." : "Cancelamento da encomenda.";
            AddMovement(flower, order.Id, line.Qty, flower.StockQuantity, reason, note);
        }

        order.StockCommitted = false;
        order.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public async Task ApplyRestoreForOrderAsync(Guid orderId, StockMovementReason reason, CancellationToken ct = default)
    {
        var order = await _uow.Orders.Query().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            return;
        await WithFlowerLocksAsync(order, inner => ApplyRestoreAsync(order, reason, inner), ct);
    }

    public void RecordAdjustment(Flower flower, int? previousQuantity, int? previousMinimum)
    {
        if (previousQuantity == flower.StockQuantity && previousMinimum == flower.MinimumStockQuantity)
            return;

        if (previousQuantity != flower.StockQuantity)
        {
            var delta = (flower.StockQuantity ?? 0) - (previousQuantity ?? 0);
            var note = flower.StockQuantity is null
                ? "Controlo de stock desligado."
                : previousQuantity is null ? "Controlo de stock ligado." : "Ajuste manual.";
            AddMovement(flower, null, delta, flower.StockQuantity, StockMovementReason.Adjustment, note);
        }

        if (flower.StockQuantity is int current && flower.MinimumStockQuantity is int minimum && current < minimum)
        {
            var wasLow = previousQuantity is int before && previousMinimum is int previousMin && before < previousMin;
            if (!wasLow)
                _pending.Add(new LowStockNotice(flower.TenantId, flower.Id, flower.Name, current, minimum));
        }
    }

    public async Task<IReadOnlyList<StockMovementDto>> ListMovementsAsync(Guid flowerId, CancellationToken ct = default)
    {
        RequireTenant();
        var exists = await _uow.Flowers.Query().AnyAsync(f => f.Id == flowerId, ct);
        if (!exists)
            throw new KeyNotFoundException("Flor não encontrada.");

        var rows = await _uow.StockMovements.Query()
            .Where(m => m.FlowerId == flowerId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
        return rows.Select(m => new StockMovementDto(m.Id, m.FlowerId, m.OrderId, m.Delta, m.BalanceAfter, m.Reason, m.Note, m.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<LowStockAlertDto>> ListLowStockAsync(CancellationToken ct = default)
    {
        RequireTenant();
        var rows = await _uow.Flowers.Query()
            .Where(f => f.IsCatalogActive && f.StockQuantity != null && f.MinimumStockQuantity != null && f.StockQuantity < f.MinimumStockQuantity)
            .OrderBy(f => f.Name)
            .Select(f => new LowStockAlertDto(f.Id, f.Name, f.StockQuantity!.Value, f.MinimumStockQuantity!.Value))
            .ToListAsync(ct);
        return rows;
    }

    public async Task FlushNoticesAsync(CancellationToken ct = default)
    {
        if (_pending.Count == 0)
            return;

        var notices = _pending.ToList();
        _pending.Clear();
        var tenantIds = notices.Select(n => n.TenantId).Distinct().ToList();
        var tenants = await _uow.Tenants.Query().Where(t => tenantIds.Contains(t.Id)).ToListAsync(ct);
        foreach (var notice in notices)
        {
            var tenant = tenants.FirstOrDefault(t => t.Id == notice.TenantId);
            var email = tenant?.ContactEmail?.Trim();
            if (tenant is not null && !string.IsNullOrWhiteSpace(email))
            {
                var (subject, html) = TransactionalEmailTemplates.LowStock(tenant, notice.FlowerName, notice.Quantity, notice.Minimum);
                await _emails.EnqueueAsync(new QueuedEmail(email, subject, html, ReplyTo: null, AuditContext: $"LowStock:{notice.FlowerId}"), ct);
            }

            _push.Enqueue(new PushWorkItem(
                notice.TenantId,
                PushAudience.TenantFlorists,
                null,
                Guid.Empty,
                "Stock baixo",
                $"{notice.FlowerName} está abaixo do mínimo ({notice.Quantity} / {notice.Minimum}).",
                "low-stock",
                NotificationPreferenceKind.LowStock));
        }
    }

    private async Task LockFlowersAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        if (!_db.Database.IsRelational() || _db.Database.CurrentTransaction is null || ids.Count == 0)
            return;

        var bytes = new byte[16];
        foreach (var id in ids)
        {
            id.TryWriteBytes(bytes);
            var key = BitConverter.ToInt64(bytes) ^ unchecked((long)0x570C411);
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", ct);
        }
    }

    private async Task EnsureItemsAsync(Order order, CancellationToken ct)
    {
        var entry = _db.Entry(order);
        if (entry.State != EntityState.Detached && !entry.Collection(o => o.Items).IsLoaded)
            await entry.Collection(o => o.Items).LoadAsync(ct);
    }

    private async Task<Dictionary<Guid, Flower>> LoadFlowersAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        if (list.Count == 0)
            return [];
        return await _uow.Flowers.Query().Where(f => list.Contains(f.Id)).ToDictionaryAsync(f => f.Id, ct);
    }

    private static List<(Guid Id, int Qty)> Lines(Order order) =>
        order.Items
            .Where(i => i.FlowerId.HasValue && i.Quantity > 0)
            .GroupBy(i => i.FlowerId!.Value)
            .Select(g => (g.Key, g.Sum(i => i.Quantity)))
            .OrderBy(l => l.Key)
            .ToList();

    private void AddMovement(Flower flower, Guid? orderId, int delta, int? balance, StockMovementReason reason, string note)
    {
        _uow.Add(new StockMovement
        {
            TenantId = flower.TenantId,
            FlowerId = flower.Id,
            OrderId = orderId,
            Delta = delta,
            BalanceAfter = balance,
            Reason = reason,
            Note = note
        });
    }

    private void QueueIfCrossed(Flower flower, int previous)
    {
        if (flower.StockQuantity is not int current || flower.MinimumStockQuantity is not int minimum)
            return;
        if (previous >= minimum && current < minimum)
            _pending.Add(new LowStockNotice(flower.TenantId, flower.Id, flower.Name, current, minimum));
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");

    private sealed record LowStockNotice(Guid TenantId, Guid FlowerId, string FlowerName, int Quantity, int Minimum);
}
