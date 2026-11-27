using FloristHub.DAL.Enums;
using FloristHub.Services.Recurring;

namespace FloristHub.Services.Tests.Recurring;

public class RecurringScheduleCalculatorTests
{
  [Fact]
    public void ShouldGenerateOrder_respects_advance_days()
    {
        var delivery = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 3, 7, 12, 0, 0, TimeSpan.Zero);
        Assert.True(RecurringScheduleCalculator.ShouldGenerateOrder(delivery, 3, now));
        Assert.False(RecurringScheduleCalculator.ShouldGenerateOrder(delivery, 3, now.AddHours(-1)));
    }

    [Fact]
    public void ComputeNextDelivery_weekly_skips_past_anchor()
    {
        var anchor = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var after = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var next = RecurringScheduleCalculator.ComputeNextDelivery(anchor, RecurringFrequency.Weekly, after);
        Assert.Equal(new DateTimeOffset(2026, 1, 22, 10, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void ComputeNextDelivery_biweekly()
    {
        var anchor = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var after = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var next = RecurringScheduleCalculator.ComputeNextDelivery(anchor, RecurringFrequency.Biweekly, after);
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void AdvanceFrom_monthly()
    {
        var current = new DateTimeOffset(2026, 1, 31, 9, 0, 0, TimeSpan.Zero);
        var next = RecurringScheduleCalculator.AdvanceFrom(current, RecurringFrequency.Monthly);
        Assert.Equal(new DateTimeOffset(2026, 2, 28, 9, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void BuildIdempotencyKey_is_stable()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var at = new DateTimeOffset(2026, 2, 1, 8, 0, 0, TimeSpan.Zero);
        Assert.Equal(RecurringScheduleCalculator.BuildIdempotencyKey(id, at),
            RecurringScheduleCalculator.BuildIdempotencyKey(id, at));
    }

    [Fact]
    public void AdvanceFrom_after_skip_moves_weekly_cycle()
    {
        var delivery = new DateTimeOffset(2026, 4, 5, 9, 0, 0, TimeSpan.Zero);
        var next = RecurringScheduleCalculator.AdvanceFrom(delivery, RecurringFrequency.Weekly);
        Assert.Equal(delivery.AddDays(7), next);
    }
}
