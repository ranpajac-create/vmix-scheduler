using VmixScheduler;

namespace VmixScheduler.Tests;

public class LogTrimmerTests
{
    [Fact]
    public void FindTrimIndex_ReturnsZero_WhenTextAtOrUnderTarget()
    {
        Assert.Equal(0, LogTrimmer.FindTrimIndex("short text", 100));
        Assert.Equal(0, LogTrimmer.FindTrimIndex(new string('a', 100), 100));
    }

    [Fact]
    public void FindTrimIndex_ReturnsZero_ForEmptyText()
    {
        Assert.Equal(0, LogTrimmer.FindTrimIndex("", 10));
    }

    [Fact]
    public void FindTrimIndex_CutsAtNextLineBoundary_NotMidLine()
    {
        // "AAAA\nBBBB\nCCCC\nDDDD\n" (20 chars) trimmed to 12 — the naive cut (index 8) lands mid
        // "BBBB", so the trim point should land just after the next '\n' instead.
        var text = "AAAA\nBBBB\nCCCC\nDDDD\n";
        var cutIndex = LogTrimmer.FindTrimIndex(text, 12);

        Assert.Equal(text.IndexOf('\n', text.Length - 12) + 1, cutIndex);
        var trimmed = text[cutIndex..];
        Assert.StartsWith("CCCC", trimmed);
        Assert.DoesNotContain("BBBB", trimmed);
    }

    [Fact]
    public void FindTrimIndex_FallsBackToRoughCut_WhenNoNewlineFound()
    {
        var text = new string('x', 500);
        var cutIndex = LogTrimmer.FindTrimIndex(text, 100);

        Assert.Equal(400, cutIndex);
    }

    [Fact]
    public void FindTrimIndex_RepeatedlyApplied_KeepsTextBounded()
    {
        // Simulates many days of one-line-per-event logging: appending well past the cap and
        // re-trimming every time it's exceeded should never let the text grow past the cap.
        var text = "";
        const int cap = 1000;
        for (int i = 0; i < 5000; i++)
        {
            text += $"[{i:D5}] some log line happened\n";
            if (text.Length > cap)
            {
                var cutIndex = LogTrimmer.FindTrimIndex(text, cap - 200);
                text = text[cutIndex..];
            }
        }

        Assert.True(text.Length <= cap, $"expected text.Length <= {cap}, was {text.Length}");
    }
}
