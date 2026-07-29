using VmixScheduler;

namespace VmixScheduler.Tests;

public class ScheduleRuleTests
{
    [Fact]
    public void Daily_ComputeOccurrence_ReturnsTodayAtTimeOfDay()
    {
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Daily, TimeOfDay = new TimeSpan(18, 0, 0) };
        var now = new DateTime(2026, 7, 29, 20, 0, 0);

        var occ = rule.ComputeOccurrence(now);

        Assert.Equal(new DateTime(2026, 7, 29, 18, 0, 0), occ);
    }

    [Fact]
    public void Daily_ComputeNextOccurrence_RollsToTomorrowIfTimeAlreadyPassed()
    {
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Daily, TimeOfDay = new TimeSpan(18, 0, 0) };
        var now = new DateTime(2026, 7, 29, 20, 0, 0);

        var next = rule.ComputeNextOccurrence(now);

        Assert.Equal(new DateTime(2026, 7, 30, 18, 0, 0), next);
    }

    [Fact]
    public void Daily_ComputeNextOccurrence_SameDayIfTimeStillAhead()
    {
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Daily, TimeOfDay = new TimeSpan(18, 0, 0) };
        var now = new DateTime(2026, 7, 29, 10, 0, 0);

        var next = rule.ComputeNextOccurrence(now);

        Assert.Equal(new DateTime(2026, 7, 29, 18, 0, 0), next);
    }

    [Fact]
    public void Weekly_ComputeOccurrence_FindsMostRecentMatchingDay()
    {
        // 2026-07-29 is a Wednesday; rule fires Mondays at 08:00 -> most recent Monday is 2026-07-27.
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Weekly, Day = DayOfWeek.Monday, TimeOfDay = new TimeSpan(8, 0, 0) };
        var now = new DateTime(2026, 7, 29, 12, 0, 0);

        var occ = rule.ComputeOccurrence(now);

        Assert.Equal(new DateTime(2026, 7, 27, 8, 0, 0), occ);
    }

    [Fact]
    public void Weekly_ComputeNextOccurrence_FindsNextMatchingDay()
    {
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Weekly, Day = DayOfWeek.Monday, TimeOfDay = new TimeSpan(8, 0, 0) };
        var now = new DateTime(2026, 7, 29, 12, 0, 0); // Wednesday

        var next = rule.ComputeNextOccurrence(now);

        Assert.Equal(new DateTime(2026, 8, 3, 8, 0, 0), next); // following Monday
    }

    [Fact]
    public void Weekly_ComputeNextOccurrence_SameDayLaterTime_StaysThisWeek()
    {
        // Today (Wed) at a time later than now should return today, not next week.
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Weekly, Day = DayOfWeek.Wednesday, TimeOfDay = new TimeSpan(18, 0, 0) };
        var now = new DateTime(2026, 7, 29, 12, 0, 0); // Wednesday

        var next = rule.ComputeNextOccurrence(now);

        Assert.Equal(new DateTime(2026, 7, 29, 18, 0, 0), next);
    }

    [Fact]
    public void Once_ComputeOccurrence_ReturnsTheAbsoluteDateTime()
    {
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Once, AbsoluteDateTime = new DateTime(2026, 12, 25, 14, 30, 0) };

        var occ = rule.ComputeOccurrence(new DateTime(2026, 12, 25, 15, 0, 0));

        Assert.Equal(new DateTime(2026, 12, 25, 14, 30, 0), occ);
    }

    [Fact]
    public void Once_ComputeNextOccurrence_NullIfAlreadyPast()
    {
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Once, AbsoluteDateTime = new DateTime(2026, 1, 1, 0, 0, 0) };

        var next = rule.ComputeNextOccurrence(new DateTime(2026, 7, 29, 0, 0, 0));

        Assert.Null(next);
    }

    [Fact]
    public void Interval_ComputeOccurrence_AlignsToWallClockBoundary()
    {
        // Every 15 minutes -> boundaries at :00, :15, :30, :45. At 10:37 the most recent boundary is 10:30.
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Interval, Interval = new TimeSpan(0, 15, 0) };
        var now = new DateTime(2026, 7, 29, 10, 37, 0);

        var occ = rule.ComputeOccurrence(now);

        Assert.Equal(new DateTime(2026, 7, 29, 10, 30, 0), occ);
    }

    [Fact]
    public void Interval_ComputeOccurrence_ExactlyOnBoundary_ReturnsThatBoundary()
    {
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Interval, Interval = new TimeSpan(0, 15, 0) };
        var now = new DateTime(2026, 7, 29, 10, 30, 0);

        var occ = rule.ComputeOccurrence(now);

        Assert.Equal(new DateTime(2026, 7, 29, 10, 30, 0), occ);
    }

    [Fact]
    public void Interval_ComputeNextOccurrence_IsOneIntervalAfterCurrentBoundary()
    {
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Interval, Interval = new TimeSpan(0, 15, 0) };
        var now = new DateTime(2026, 7, 29, 10, 37, 0);

        var next = rule.ComputeNextOccurrence(now);

        Assert.Equal(new DateTime(2026, 7, 29, 10, 45, 0), next);
    }

    [Fact]
    public void Interval_ZeroOrNegativeInterval_ReturnsNull()
    {
        var rule = new ScheduleRule { Recurrence = RuleRecurrence.Interval, Interval = TimeSpan.Zero };

        Assert.Null(rule.ComputeOccurrence(DateTime.Now));
        Assert.Null(rule.ComputeNextOccurrence(DateTime.Now));
    }

    [Fact]
    public void RecurrenceDisplay_FormatsEachRecurrenceKind()
    {
        Assert.Equal(
            "Daily @ 18:00:00",
            new ScheduleRule { Recurrence = RuleRecurrence.Daily, TimeOfDay = new TimeSpan(18, 0, 0) }.RecurrenceDisplay);

        Assert.Equal(
            "Monday @ 08:00:00",
            new ScheduleRule { Recurrence = RuleRecurrence.Weekly, Day = DayOfWeek.Monday, TimeOfDay = new TimeSpan(8, 0, 0) }.RecurrenceDisplay);

        Assert.Equal(
            "Every 00:15:00",
            new ScheduleRule { Recurrence = RuleRecurrence.Interval, Interval = new TimeSpan(0, 15, 0) }.RecurrenceDisplay);
    }
}
