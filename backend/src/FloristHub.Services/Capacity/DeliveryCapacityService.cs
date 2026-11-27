using FloristHub.DAL.Audit;
using FloristHub.DAL.Entities;
using FloristHub.DAL.MultiTenancy;
using FloristHub.DAL.Persistence;
using FloristHub.DAL.UnitOfWork;
using FloristHub.Services.DTOs.Capacity;
using FloristHub.Services.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace FloristHub.Services.Capacity;

public interface IDeliveryCapacityService
{
    Task<DeliveryCapacitySettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task<DeliveryCapacitySettingsDto> UpdateSettingsAsync(UpdateDeliveryCapacitySettingsRequest request, Guid userId, CancellationToken ct = default);
    Task<DeliveryExceptionDto> UpsertExceptionAsync(UpsertDeliveryCapacityExceptionRequest request, Guid userId, CancellationToken ct = default);
    Task DeleteExceptionAsync(DateOnly date, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<FloristCapacityDayDto>> GetFloristCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IReadOnlyList<PublicCapacityDayDto>> GetPublicCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task RunLockedAsync(Guid tenantId, DateOnly? date, Func<CancellationToken, Task> action, CancellationToken ct = default);
    Task ApplyReserveAsync(Order order, Tenant tenant, CancellationToken ct = default);
    Task ApplyReleaseAsync(Order order, CancellationToken ct = default);
}

public sealed class DeliveryCapacityService : IDeliveryCapacityService
{
    private readonly IUnitOfWork _uow;
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;
    private readonly IRelationalWork _work;
    private readonly CapacityGate _gate;

    public DeliveryCapacityService(
        IUnitOfWork uow,
        ApplicationDbContext db,
        ITenantContext tenant,
        IAuditLogger audit,
        IRelationalWork work,
        CapacityGate gate)
    {
        _uow = uow;
        _db = db;
        _tenant = tenant;
        _audit = audit;
        _work = work;
        _gate = gate;
    }

    public async Task<DeliveryCapacitySettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        RequireTenant();
        var policy = await _uow.CapacityPolicies.Query().FirstOrDefaultAsync(ct);
        var windows = await _uow.CapacityWindows.Query().OrderBy(w => w.SortOrder).ToListAsync(ct);
        var exceptions = await _uow.CapacityExceptions.Query().OrderBy(e => e.ServiceDate).ToListAsync(ct);
        return MapSettings(policy, windows, exceptions);
    }

    public async Task<DeliveryCapacitySettingsDto> UpdateSettingsAsync(
        UpdateDeliveryCapacitySettingsRequest request,
        Guid userId,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        CapacityRules.EnsureDailyLimit(request.DailyOrderLimit);
        var parsed = ParseWindows(request.Windows ?? []);
        CapacityRules.EnsureWindowsValid(parsed.Select(w => (w.Start, w.End, w.Limit, w.Label)).ToList());

        var policy = await _uow.CapacityPolicies.Query().FirstOrDefaultAsync(ct);
        if (policy is null)
        {
            policy = new DeliveryCapacityPolicy { TenantId = tenantId, DailyOrderLimit = request.DailyOrderLimit };
            await _uow.CapacityPolicies.AddAsync(policy, ct);
        }
        else
        {
            policy.DailyOrderLimit = request.DailyOrderLimit;
            policy.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var existing = await _uow.CapacityWindows.Query().ToListAsync(ct);
        foreach (var window in existing)
            _uow.CapacityWindows.Remove(window);

        for (var i = 0; i < parsed.Count; i++)
        {
            var window = parsed[i];
            await _uow.CapacityWindows.AddAsync(new DeliveryCapacityWindow
            {
                TenantId = tenantId,
                Label = window.Label,
                StartTime = window.Start,
                EndTime = window.End,
                OrderLimit = window.Limit,
                SortOrder = i
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("DeliveryCapacityUpdated", userId, tenantId, nameof(DeliveryCapacityPolicy), policy.Id, $"Limite={request.DailyOrderLimit}", ct);
        return await GetSettingsAsync(ct);
    }

    public async Task<DeliveryExceptionDto> UpsertExceptionAsync(
        UpsertDeliveryCapacityExceptionRequest request,
        Guid userId,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (request.DailyOrderLimitOverride is int limit)
            CapacityRules.EnsureDailyLimit(limit);
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note is { Length: > 400 })
            throw new InvalidOperationException("A nota da exceção pode ter no máximo 400 caracteres.");

        var row = await _uow.CapacityExceptions.Query().FirstOrDefaultAsync(e => e.ServiceDate == request.Date, ct);
        if (row is null)
        {
            row = new DeliveryCapacityException
            {
                TenantId = tenantId,
                ServiceDate = request.Date,
                IsClosed = request.IsClosed,
                DailyOrderLimitOverride = request.IsClosed ? null : request.DailyOrderLimitOverride,
                Note = note
            };
            await _uow.CapacityExceptions.AddAsync(row, ct);
        }
        else
        {
            row.IsClosed = request.IsClosed;
            row.DailyOrderLimitOverride = request.IsClosed ? null : request.DailyOrderLimitOverride;
            row.Note = note;
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("DeliveryCapacityException", userId, tenantId, nameof(DeliveryCapacityException), row.Id, request.Date.ToString("yyyy-MM-dd"), ct);
        return new DeliveryExceptionDto(row.ServiceDate, row.IsClosed, row.DailyOrderLimitOverride, row.Note);
    }

    public async Task DeleteExceptionAsync(DateOnly date, Guid userId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var row = await _uow.CapacityExceptions.Query().FirstOrDefaultAsync(e => e.ServiceDate == date, ct)
            ?? throw new KeyNotFoundException("Não há exceção nesta data.");
        _uow.CapacityExceptions.Remove(row);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("DeliveryCapacityExceptionRemoved", userId, tenantId, nameof(DeliveryCapacityException), row.Id, date.ToString("yyyy-MM-dd"), ct);
    }

    public async Task<IReadOnlyList<FloristCapacityDayDto>> GetFloristCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        RequireTenant();
        EnsureRange(from, to);
        var days = await BuildDaysAsync(from, to, ct);
        return days.Select(d => new FloristCapacityDayDto(d.Date, d.Closed, d.DailyLimit, d.Booked, d.Remaining, d.Note, d.Windows)).ToList();
    }

    public async Task<IReadOnlyList<PublicCapacityDayDto>> GetPublicCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await TenantPublicAccessRules.RequirePublishedTenantAsync(_tenant, _uow, ct);
        EnsureRange(from, to);
        var days = await BuildDaysAsync(from, to, ct);
        return days.Select(d => new PublicCapacityDayDto(d.Date, d.Closed, d.DailyLimit, d.Booked, d.Remaining, d.Windows)).ToList();
    }

    public async Task RunLockedAsync(Guid tenantId, DateOnly? date, Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        if (date is null)
        {
            await _work.RunAsync(action, ct);
            return;
        }

        var gate = _gate.For(tenantId, date.Value);
        await gate.WaitAsync(ct);
        try
        {
            await _work.RunAsync(async inner =>
            {
                if (_db.Database.IsRelational() && _db.Database.CurrentTransaction is not null)
                {
                    var key = CapacityRules.AdvisoryKey(tenantId, date.Value);
                    await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", inner);
                }

                await action(inner);
            }, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task ApplyReserveAsync(Order order, Tenant tenant, CancellationToken ct = default)
    {
        var date = CapacityRules.DateOf(order.RequestedDeliveryAt);
        if (date is null)
            return;

        var policy = await _uow.CapacityPolicies.Query().FirstOrDefaultAsync(ct);
        if (policy is null)
            return;

        var exception = await _uow.CapacityExceptions.Query().FirstOrDefaultAsync(e => e.ServiceDate == date.Value, ct);
        if (exception?.IsClosed == true)
            throw new InvalidOperationException("Este dia está indisponível para entregas (folga ou feriado).");

        var dailyLimit = exception?.DailyOrderLimitOverride ?? policy.DailyOrderLimit;
        var windows = await _uow.CapacityWindows.Query().OrderBy(w => w.SortOrder).ToListAsync(ct);
        DeliveryCapacityWindow? window = null;
        if (windows.Count > 0)
        {
            window = CapacityRules.MatchWindow(windows, CapacityRules.TimeOf(order.RequestedDeliveryAt!.Value));
            if (window is null)
                throw new InvalidOperationException("A hora escolhida está fora das janelas de entrega.");
        }

        await IncrementAsync(order.TenantId, date.Value, DeliveryCapacityHold.DayScope, dailyLimit, "Não há vagas para esta data.", ct);
        if (window is not null)
            await IncrementAsync(order.TenantId, date.Value, window.Key, window.OrderLimit, "Não há vagas nesta janela de entrega.", ct);

        order.CapacityServiceDate = date;
        order.CapacityWindowKey = window?.Key;
        order.CapacityHeld = true;
    }

    public async Task ApplyReleaseAsync(Order order, CancellationToken ct = default)
    {
        if (!order.CapacityHeld || order.CapacityServiceDate is null)
            return;

        await DecrementAsync(order.TenantId, order.CapacityServiceDate.Value, DeliveryCapacityHold.DayScope, ct);
        if (!string.IsNullOrWhiteSpace(order.CapacityWindowKey))
            await DecrementAsync(order.TenantId, order.CapacityServiceDate.Value, order.CapacityWindowKey, ct);

        order.CapacityHeld = false;
        order.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task IncrementAsync(Guid tenantId, DateOnly date, string scope, int limit, string fullMessage, CancellationToken ct)
    {
        var hold = await _uow.CapacityHolds.Query()
            .FirstOrDefaultAsync(h => h.ServiceDate == date && h.ScopeKey == scope, ct);
        if (hold is null)
        {
            hold = new DeliveryCapacityHold
            {
                TenantId = tenantId,
                ServiceDate = date,
                ScopeKey = scope,
                BookedCount = 0
            };
            await _uow.CapacityHolds.AddAsync(hold, ct);
        }

        if (hold.BookedCount >= limit)
            throw new InvalidOperationException(fullMessage);

        hold.BookedCount++;
        hold.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task DecrementAsync(Guid tenantId, DateOnly date, string scope, CancellationToken ct)
    {
        var hold = await _uow.CapacityHolds.Query()
            .FirstOrDefaultAsync(h => h.TenantId == tenantId && h.ServiceDate == date && h.ScopeKey == scope, ct);
        if (hold is null || hold.BookedCount <= 0)
            return;
        hold.BookedCount--;
        hold.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task<List<DayBuild>> BuildDaysAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var policy = await _uow.CapacityPolicies.Query().AsNoTracking().FirstOrDefaultAsync(ct);
        var windows = await _uow.CapacityWindows.Query().AsNoTracking().OrderBy(w => w.SortOrder).ToListAsync(ct);
        var exceptions = await _uow.CapacityExceptions.Query().AsNoTracking()
            .Where(e => e.ServiceDate >= from && e.ServiceDate <= to)
            .ToListAsync(ct);
        var holds = await _uow.CapacityHolds.Query().AsNoTracking()
            .Where(h => h.ServiceDate >= from && h.ServiceDate <= to)
            .ToListAsync(ct);

        var days = new List<DayBuild>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var exception = exceptions.FirstOrDefault(e => e.ServiceDate == date);
            var closed = exception?.IsClosed == true;
            int? dailyLimit = policy is null ? null : exception?.DailyOrderLimitOverride ?? policy.DailyOrderLimit;
            var booked = holds.FirstOrDefault(h => h.ServiceDate == date && h.ScopeKey == DeliveryCapacityHold.DayScope)?.BookedCount ?? 0;
            int? remaining = closed ? 0 : dailyLimit is null ? null : Math.Max(0, dailyLimit.Value - booked);
            var windowDtos = windows.Select(w =>
            {
                var windowBooked = holds.FirstOrDefault(h => h.ServiceDate == date && h.ScopeKey == w.Key)?.BookedCount ?? 0;
                var windowRemaining = closed ? 0 : Math.Max(0, w.OrderLimit - windowBooked);
                return new PublicCapacityWindowDto(w.Label, w.StartTime.ToString("HH:mm"), w.EndTime.ToString("HH:mm"), w.OrderLimit, windowBooked, windowRemaining);
            }).ToList();

            if (remaining is > 0 && windowDtos.Count > 0 && windowDtos.All(w => w.Remaining == 0))
                remaining = 0;

            days.Add(new DayBuild(date, closed, dailyLimit, booked, remaining, exception?.Note, windowDtos));
        }

        return days;
    }

    private static void EnsureRange(DateOnly from, DateOnly to)
    {
        if (to < from)
            throw new InvalidOperationException("O intervalo de datas é inválido.");
        if (to.DayNumber - from.DayNumber > 120)
            throw new InvalidOperationException("O calendário pode cobrir no máximo 121 dias.");
    }

    private static List<(string Label, TimeOnly Start, TimeOnly End, int Limit)> ParseWindows(IReadOnlyList<DeliveryWindowInput> windows)
    {
        var parsed = new List<(string Label, TimeOnly Start, TimeOnly End, int Limit)>();
        foreach (var window in windows)
        {
            if (!TimeOnly.TryParse(window.Start, out var start) || !TimeOnly.TryParse(window.End, out var end))
                throw new InvalidOperationException("Use horas no formato HH:mm.");
            parsed.Add((window.Label?.Trim() ?? "", start, end, window.OrderLimit));
        }

        return parsed;
    }

    private static DeliveryCapacitySettingsDto MapSettings(
        DeliveryCapacityPolicy? policy,
        IReadOnlyList<DeliveryCapacityWindow> windows,
        IReadOnlyList<DeliveryCapacityException> exceptions) =>
        new(
            policy?.DailyOrderLimit,
            windows.Select(w => new DeliveryWindowDto(w.Label, w.StartTime.ToString("HH:mm"), w.EndTime.ToString("HH:mm"), w.OrderLimit)).ToList(),
            exceptions.Select(e => new DeliveryExceptionDto(e.ServiceDate, e.IsClosed, e.DailyOrderLimitOverride, e.Note)).ToList());

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("Tenant não resolvido.");

    private sealed record DayBuild(
        DateOnly Date,
        bool Closed,
        int? DailyLimit,
        int Booked,
        int? Remaining,
        string? Note,
        IReadOnlyList<PublicCapacityWindowDto> Windows);
}
