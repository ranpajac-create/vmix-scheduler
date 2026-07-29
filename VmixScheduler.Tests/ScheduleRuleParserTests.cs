using VmixScheduler;

namespace VmixScheduler.Tests;

public class ScheduleRuleParserTests
{
    private static VmixInput Input(string name) => new() { Key = "k1", Title = name, ShortTitle = "" };

    [Fact]
    public void DailyProgram_ParsesTimeOfDay()
    {
        var rule = ScheduleRuleParser.Parse(Input("Daily@19:00:00"));

        Assert.NotNull(rule);
        Assert.Equal(ScheduleCategory.Program, rule!.Category);
        Assert.Equal(RuleRecurrence.Daily, rule.Recurrence);
        Assert.Equal(new TimeSpan(19, 0, 0), rule.TimeOfDay);
        Assert.Equal("Daily@19:00:00", rule.DisplayName);
    }

    [Fact]
    public void DailyProgram_WithLabel_UsesLabelAsDisplayName()
    {
        var rule = ScheduleRuleParser.Parse(Input("Evening Movie Daily@19:00:00"));

        Assert.NotNull(rule);
        Assert.Equal("Evening Movie", rule!.DisplayName);
        Assert.Equal("Evening Movie Daily@19:00:00", rule.RawTitle);
    }

    [Fact]
    public void Ad_DailyAt_ParsesAsAdCategory()
    {
        var rule = ScheduleRuleParser.Parse(Input("Ad@16:30:00"));

        Assert.NotNull(rule);
        Assert.Equal(ScheduleCategory.Ad, rule!.Category);
        Assert.Equal(RuleRecurrence.Daily, rule.Recurrence);
        Assert.Equal(new TimeSpan(16, 30, 0), rule.TimeOfDay);
    }

    [Fact]
    public void Ad_Every_ParsesAsIntervalRecurrence()
    {
        var rule = ScheduleRuleParser.Parse(Input("Ad@Every00:15:00"));

        Assert.NotNull(rule);
        Assert.Equal(ScheduleCategory.Ad, rule!.Category);
        Assert.Equal(RuleRecurrence.Interval, rule.Recurrence);
        Assert.Equal(new TimeSpan(0, 15, 0), rule.Interval);
    }

    [Fact]
    public void LShapeAd_DailyAt_ParsesAsLShapeAdCategory()
    {
        var rule = ScheduleRuleParser.Parse(Input("L@20:00:00"));

        Assert.NotNull(rule);
        Assert.Equal(ScheduleCategory.LShapeAd, rule!.Category);
        Assert.Equal(RuleRecurrence.Daily, rule.Recurrence);
    }

    [Fact]
    public void LShapeAd_Every_ParsesAsIntervalRecurrence()
    {
        var rule = ScheduleRuleParser.Parse(Input("L@Every01:00:00"));

        Assert.NotNull(rule);
        Assert.Equal(ScheduleCategory.LShapeAd, rule!.Category);
        Assert.Equal(RuleRecurrence.Interval, rule.Recurrence);
        Assert.Equal(new TimeSpan(1, 0, 0), rule.Interval);
    }

    [Theory]
    [InlineData("Mon@08:00:00", DayOfWeek.Monday)]
    [InlineData("Tue@08:00:00", DayOfWeek.Tuesday)]
    [InlineData("Wed@08:00:00", DayOfWeek.Wednesday)]
    [InlineData("Thu@08:00:00", DayOfWeek.Thursday)]
    [InlineData("Fri@08:00:00", DayOfWeek.Friday)]
    [InlineData("Sat@08:00:00", DayOfWeek.Saturday)]
    [InlineData("Sun@08:00:00", DayOfWeek.Sunday)]
    public void WeeklyProgram_ParsesCorrectDay(string title, DayOfWeek expectedDay)
    {
        var rule = ScheduleRuleParser.Parse(Input(title));

        Assert.NotNull(rule);
        Assert.Equal(ScheduleCategory.Program, rule!.Category);
        Assert.Equal(RuleRecurrence.Weekly, rule.Recurrence);
        Assert.Equal(expectedDay, rule.Day);
    }

    [Fact]
    public void SponsorAd_ParsesAsWeeklySponsorCategory()
    {
        var rule = ScheduleRuleParser.Parse(Input("Spon-Wed@12:00:00"));

        Assert.NotNull(rule);
        Assert.Equal(ScheduleCategory.Sponsor, rule!.Category);
        Assert.Equal(RuleRecurrence.Weekly, rule.Recurrence);
        Assert.Equal(DayOfWeek.Wednesday, rule.Day);
    }

    [Fact]
    public void AbsoluteDateTime_ParsesAsOnceProgram()
    {
        var rule = ScheduleRuleParser.Parse(Input("14:30:00/2026-12-25"));

        Assert.NotNull(rule);
        Assert.Equal(ScheduleCategory.Program, rule!.Category);
        Assert.Equal(RuleRecurrence.Once, rule.Recurrence);
        Assert.Equal(new DateTime(2026, 12, 25, 14, 30, 0), rule.AbsoluteDateTime);
    }

    [Fact]
    public void AbsoluteDateTime_WithLabel_UsesLabelAsDisplayName()
    {
        var rule = ScheduleRuleParser.Parse(Input("Christmas Special 14:30:00/2026-12-25"));

        Assert.NotNull(rule);
        Assert.Equal("Christmas Special", rule!.DisplayName);
    }

    [Fact]
    public void ParsingIsCaseInsensitive()
    {
        var rule = ScheduleRuleParser.Parse(Input("ad@16:30:00"));

        Assert.NotNull(rule);
        Assert.Equal(ScheduleCategory.Ad, rule!.Category);
    }

    [Theory]
    [InlineData("Filler")]
    [InlineData("Now")]
    [InlineData("Overlay1")]
    [InlineData("")]
    [InlineData("Random Video Name.mp4")]
    public void NonMatchingNames_ReturnNull(string title)
    {
        var rule = ScheduleRuleParser.Parse(Input(title));

        Assert.Null(rule);
    }

    [Fact]
    public void SponsorPattern_TakesPrecedenceOverWeeklyPattern()
    {
        // "Spon-Mon@..." must not be misparsed as a plain weekly Program just because "Mon@..." is a suffix match.
        var rule = ScheduleRuleParser.Parse(Input("Spon-Mon@09:00:00"));

        Assert.NotNull(rule);
        Assert.Equal(ScheduleCategory.Sponsor, rule!.Category);
    }
}
