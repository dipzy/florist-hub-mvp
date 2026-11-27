using FloristHub.DAL.Enums;

namespace FloristHub.Services.Recurring;

public static class RecurringScheduleCalculator
{
    public static string BuildIdempotencyKey(Guid subscriptionId, DateTimeOffset deliveryAt) =>
        $"{subscriptionId:N}:{deliveryAt.UtcDateTime:O}";

    public static bool ShouldGenerateOrder(DateTimeOffset deliveryAt, int advanceDays, DateTimeOffset utcNow) =>
        deliveryAt.AddDays(-advanceDays) <= utcNow;

    public static DateTimeOffset ComputeNextDelivery(
        DateTimeOffset anchor,
        RecurringFrequency frequency,
        DateTimeOffset after)
    {
        var cursor = anchor;
        if (cursor <= after)
        {
            cursor = frequency switch
            {
                RecurringFrequency.Weekly => AdvanceWeeks(cursor, after, 1),
                RecurringFrequency.Biweekly => AdvanceWeeks(cursor, after, 2),
                RecurringFrequency.Monthly => AdvanceMonths(cursor, after),
                _ => cursor.AddDays(7)
            };
        }

        return cursor;
    }

    public static DateTimeOffset AdvanceFrom(DateTimeOffset current, RecurringFrequency frequency) =>
        frequency switch
        {
            RecurringFrequency.Weekly => current.AddDays(7),
            RecurringFrequency.Biweekly => current.AddDays(14),
            RecurringFrequency.Monthly => current.AddMonths(1),
            _ => current.AddDays(7)
        };

    private static DateTimeOffset AdvanceWeeks(DateTimeOffset anchor, DateTimeOffset after, int weeks)
    {
        var cursor = anchor;
        var step = TimeSpan.FromDays(7 * weeks);
        while (cursor <= after)
            cursor = cursor.Add(step);
        return cursor;
    }

    private static DateTimeOffset AdvanceMonths(DateTimeOffset anchor, DateTimeOffset after)
    {
        var cursor = anchor;
        while (cursor <= after)
            cursor = cursor.AddMonths(1);
        return cursor;
    }
}
