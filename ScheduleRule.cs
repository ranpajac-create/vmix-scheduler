namespace VmixScheduler;

public enum ScheduleCategory
{
    Program,
    Ad,
    LShapeAd,
    Sponsor
}

public enum RuleRecurrence
{
    Once,
    Daily,
    Weekly,
    Interval
}

public class ScheduleRule
{
    public string InputKey { get; set; } = "";
    public string RawTitle { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public ScheduleCategory Category { get; set; }
    public RuleRecurrence Recurrence { get; set; }
    public DayOfWeek? Day { get; set; }
    public TimeSpan TimeOfDay { get; set; }
    public TimeSpan Interval { get; set; }
    public DateTime? AbsoluteDateTime { get; set; }

    /// <summary>The exact occurrence instant that was already fired, so the same slot doesn't re-trigger.</summary>
    public DateTime? LastFiredOccurrence { get; set; }

    public string RecurrenceDisplay => Recurrence switch
    {
        RuleRecurrence.Once => AbsoluteDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
        RuleRecurrence.Daily => $"Daily @ {TimeOfDay:hh\\:mm\\:ss}",
        RuleRecurrence.Weekly => $"{Day} @ {TimeOfDay:hh\\:mm\\:ss}",
        RuleRecurrence.Interval => $"Every {Interval:hh\\:mm\\:ss}",
        _ => ""
    };

    /// <summary>The most recent scheduled instant at or before "now" (may be well in the past).</summary>
    public DateTime? ComputeOccurrence(DateTime now)
    {
        switch (Recurrence)
        {
            case RuleRecurrence.Once:
                return AbsoluteDateTime;

            case RuleRecurrence.Daily:
                return now.Date + TimeOfDay;

            case RuleRecurrence.Weekly:
                for (int i = 0; i < 7; i++)
                {
                    var candidate = now.Date.AddDays(-i);
                    if (candidate.DayOfWeek == Day)
                        return candidate + TimeOfDay;
                }
                return null;

            case RuleRecurrence.Interval:
                if (Interval <= TimeSpan.Zero) return null;
                var totalSeconds = now.TimeOfDay.TotalSeconds;
                var intervalSeconds = Interval.TotalSeconds;
                var occurrenceSeconds = Math.Floor(totalSeconds / intervalSeconds) * intervalSeconds;
                return now.Date + TimeSpan.FromSeconds(occurrenceSeconds);

            default:
                return null;
        }
    }

    /// <summary>The next strictly-future scheduled instant, for display purposes.</summary>
    public DateTime? ComputeNextOccurrence(DateTime now)
    {
        switch (Recurrence)
        {
            case RuleRecurrence.Once:
                return AbsoluteDateTime > now ? AbsoluteDateTime : null;

            case RuleRecurrence.Daily:
                var todayAt = now.Date + TimeOfDay;
                return todayAt > now ? todayAt : todayAt.AddDays(1);

            case RuleRecurrence.Weekly:
                for (int i = 0; i < 8; i++)
                {
                    var candidateDate = now.Date.AddDays(i);
                    var candidate = candidateDate + TimeOfDay;
                    if (candidate > now && candidateDate.DayOfWeek == Day)
                        return candidate;
                }
                return null;

            case RuleRecurrence.Interval:
                var occ = ComputeOccurrence(now);
                return occ.HasValue ? occ.Value + Interval : null;

            default:
                return null;
        }
    }
}
