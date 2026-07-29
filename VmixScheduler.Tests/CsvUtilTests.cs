using VmixScheduler;

namespace VmixScheduler.Tests;

public class CsvUtilTests
{
    [Fact]
    public void EscapeField_LeavesPlainFieldUnchanged()
    {
        Assert.Equal("Ad@16:30:00", CsvUtil.EscapeField("Ad@16:30:00"));
    }

    [Fact]
    public void EscapeField_WrapsFieldContainingComma()
    {
        Assert.Equal("\"Smith, John\"", CsvUtil.EscapeField("Smith, John"));
    }

    [Fact]
    public void EscapeField_DoublesInternalQuotes()
    {
        Assert.Equal("\"He said \"\"hi\"\"\"", CsvUtil.EscapeField("He said \"hi\""));
    }

    [Fact]
    public void ParseLine_SplitsPlainFields()
    {
        var fields = CsvUtil.ParseLine("2026-07-29 16:30:00,Scheduled,Ad,Evening Ad,Ad@16:30:00");

        Assert.Equal(new[] { "2026-07-29 16:30:00", "Scheduled", "Ad", "Evening Ad", "Ad@16:30:00" }, fields);
    }

    [Fact]
    public void ParseLine_HandlesQuotedFieldWithEmbeddedComma()
    {
        var fields = CsvUtil.ParseLine("2026-07-29 16:30:00,Manual,Program,\"Smith, John Show\",RawTitle");

        Assert.Equal(new[] { "2026-07-29 16:30:00", "Manual", "Program", "Smith, John Show", "RawTitle" }, fields);
    }

    [Fact]
    public void ParseLine_HandlesDoubledQuotesInsideQuotedField()
    {
        var fields = CsvUtil.ParseLine("t,Manual,Program,\"He said \"\"hi\"\"\",raw");

        Assert.Equal("He said \"hi\"", fields[3]);
    }

    [Theory]
    [InlineData("plain text")]
    [InlineData("has, a comma")]
    [InlineData("has \"quotes\" inside")]
    [InlineData("has, both \"kinds\"")]
    public void EscapeThenParse_RoundTrips(string original)
    {
        var escaped = CsvUtil.EscapeField(original);
        var line = string.Join(",", CsvUtil.EscapeField("2026-07-29 00:00:00"), escaped, CsvUtil.EscapeField("x"));

        var fields = CsvUtil.ParseLine(line);

        Assert.Equal(original, fields[1]);
    }
}
