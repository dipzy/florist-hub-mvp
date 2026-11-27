using FloristHub.DAL.Entities;

namespace FloristHub.Services.Capacity;

public static class CapacityRules
{
    public const int MaxDailyLimit = 500;
    public const int MaxWindows = 8;

    public static DateOnly? DateOf(DateTimeOffset? requested)
    {
        if (requested is null)
            return null;
        return DateOnly.FromDateTime(requested.Value.DateTime);
    }

    public static TimeOnly TimeOf(DateTimeOffset requested) => TimeOnly.FromTimeSpan(requested.TimeOfDay);

    public static DeliveryCapacityWindow? MatchWindow(IEnumerable<DeliveryCapacityWindow> windows, TimeOnly time) =>
        windows
            .OrderBy(w => w.SortOrder)
            .ThenBy(w => w.StartTime)
            .FirstOrDefault(w => time >= w.StartTime && time <= w.EndTime);

    public static void EnsureWindowsValid(IReadOnlyList<(TimeOnly Start, TimeOnly End, int Limit, string Label)> windows)
    {
        if (windows.Count > MaxWindows)
            throw new InvalidOperationException($"No máximo {MaxWindows} janelas de entrega.");

        foreach (var window in windows)
        {
            if (string.IsNullOrWhiteSpace(window.Label) || window.Label.Trim().Length > 40)
                throw new InvalidOperationException("Cada janela precisa de um nome até 40 caracteres.");
            if (window.Start >= window.End)
                throw new InvalidOperationException("A hora de início da janela tem de ser anterior à hora de fim.");
            if (window.Limit is < 0 or > MaxDailyLimit)
                throw new InvalidOperationException($"O limite da janela tem de estar entre 0 e {MaxDailyLimit}.");
        }

        var ordered = windows.OrderBy(w => w.Start).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].Start <= ordered[i - 1].End)
                throw new InvalidOperationException("As janelas não se podem sobrepor. Ex.: 09:00–11:59 e 12:00–19:00.");
        }
    }

    public static void EnsureDailyLimit(int limit)
    {
        if (limit is < 0 or > MaxDailyLimit)
            throw new InvalidOperationException($"O limite diário tem de estar entre 0 e {MaxDailyLimit}.");
    }

    public static long AdvisoryKey(Guid tenantId, DateOnly date)
    {
        Span<byte> bytes = stackalloc byte[16];
        tenantId.TryWriteBytes(bytes);
        var hi = BitConverter.ToInt64(bytes);
        return hi ^ ((long)date.DayNumber << 1);
    }
}
