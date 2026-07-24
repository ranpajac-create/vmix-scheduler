using System.Text.RegularExpressions;

namespace VmixScheduler;

/// <summary>
/// Parses vTimer-style naming conventions out of vMix input titles:
///   HH:MM:SS/YYYY-MM-DD        one-off program
///   Daily@HH:MM:SS              daily program
///   Ad@HH:MM:SS                 daily ad
///   Ad@EveryHH:MM:SS            repeating ad (every interval, all day)
///   L@HH:MM:SS                  daily L-shape ad
///   L@EveryHH:MM:SS             repeating L-shape ad
///   Mon@HH:MM:SS                weekly program (Mon/Tue/Wed/Thu/Fri/Sat/Sun)
///   Spon-Mon@HH:MM:SS           weekly sponsor ad
/// An optional free-text label may precede the code (e.g. "MovieName Daily@19:00:00");
/// that label becomes the rule's DisplayName.
/// </summary>
public static class ScheduleRuleParser
{
    private const string Time = @"(?<h>\d{1,2}):(?<m>\d{1,2}):(?<s>\d{1,2})";

    private static readonly (string Pattern, ScheduleCategory Category, RuleRecurrence Recurrence)[] Patterns =
    {
        ($@"^(?<label>.*?)\s*Spon-(?<day>Mon|Tue|Wed|Thu|Fri|Sat|Sun)@{Time}$", ScheduleCategory.Sponsor, RuleRecurrence.Weekly),
        ($@"^(?<label>.*?)\s*(?<day>Mon|Tue|Wed|Thu|Fri|Sat|Sun)@{Time}$", ScheduleCategory.Program, RuleRecurrence.Weekly),
        ($@"^(?<label>.*?)\s*Ad@Every{Time}$", ScheduleCategory.Ad, RuleRecurrence.Interval),
        ($@"^(?<label>.*?)\s*Ad@{Time}$", ScheduleCategory.Ad, RuleRecurrence.Daily),
        ($@"^(?<label>.*?)\s*L@Every{Time}$", ScheduleCategory.LShapeAd, RuleRecurrence.Interval),
        ($@"^(?<label>.*?)\s*L@{Time}$", ScheduleCategory.LShapeAd, RuleRecurrence.Daily),
        ($@"^(?<label>.*?)\s*Daily@{Time}$", ScheduleCategory.Program, RuleRecurrence.Daily),
    };

    private static readonly Regex AbsolutePattern = new(
        $@"^(?<label>.*?)\s*{Time}/(?<y>\d{{4}})-(?<mo>\d{{1,2}})-(?<d>\d{{1,2}})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static ScheduleRule? Parse(VmixInput input)
    {
        var title = input.Name.Trim();
        if (title.Length == 0) return null;

        foreach (var (pattern, category, recurrence) in Patterns)
        {
            var match = Regex.Match(title, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            var rule = new ScheduleRule
            {
                InputKey = input.Key,
                RawTitle = title,
                DisplayName = NonEmpty(match.Groups["label"].Value, title),
                Category = category,
                Recurrence = recurrence
            };

            if (recurrence == RuleRecurrence.Interval)
                rule.Interval = ParseTime(match);
            else
                rule.TimeOfDay = ParseTime(match);

            if (recurrence == RuleRecurrence.Weekly)
                rule.Day = ParseDay(match.Groups["day"].Value);

            return rule;
        }

        var abs = AbsolutePattern.Match(title);
        if (abs.Success)
        {
            var year = int.Parse(abs.Groups["y"].Value);
            var month = int.Parse(abs.Groups["mo"].Value);
            var day = int.Parse(abs.Groups["d"].Value);
            var time = ParseTime(abs);

            return new ScheduleRule
            {
                InputKey = input.Key,
                RawTitle = title,
                DisplayName = NonEmpty(abs.Groups["label"].Value, title),
                Category = ScheduleCategory.Program,
                Recurrence = RuleRecurrence.Once,
                AbsoluteDateTime = new DateTime(year, month, day) + time
            };
        }

        return null;
    }

    private static TimeSpan ParseTime(Match m) =>
        new(int.Parse(m.Groups["h"].Value), int.Parse(m.Groups["m"].Value), int.Parse(m.Groups["s"].Value));

    private static string NonEmpty(string label, string fallback) =>
        string.IsNullOrWhiteSpace(label) ? fallback : label.Trim();

    private static DayOfWeek ParseDay(string s) => s.Substring(0, 3).ToLowerInvariant() switch
    {
        "sun" => DayOfWeek.Sunday,
        "mon" => DayOfWeek.Monday,
        "tue" => DayOfWeek.Tuesday,
        "wed" => DayOfWeek.Wednesday,
        "thu" => DayOfWeek.Thursday,
        "fri" => DayOfWeek.Friday,
        "sat" => DayOfWeek.Saturday,
        _ => throw new ArgumentException($"Unknown day abbreviation: {s}")
    };
}
