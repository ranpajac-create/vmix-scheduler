namespace VmixScheduler;

/// <summary>
/// Pure trim-point math for the Log panel's TextBox. Left running for days, an append-only
/// TextBox.AppendText call every time something happens (scheduled fires, ad breaks, overlay
/// pops, retried errors...) grows the backing string without bound — degrading UI responsiveness
/// and memory over a multi-day broadcast even though nothing ever crashes outright. Form1 caps the
/// panel by trimming from the front once it passes a size threshold; the trim-point calculation
/// lives here (not inline in Form1) so it's unit-testable without a live TextBox.
/// </summary>
public static class LogTrimmer
{
    /// <summary>
    /// Finds where to cut <paramref name="text"/> so at most roughly <paramref name="targetChars"/>
    /// remain, without slicing a line in half — it lands on the next line boundary at or after the
    /// naive cut point instead of the exact character count. Returns 0 (no trim) when the text is
    /// already at or under the target.
    /// </summary>
    public static int FindTrimIndex(string text, int targetChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= targetChars) return 0;

        var roughCut = text.Length - targetChars;
        var newlineIndex = text.IndexOf('\n', roughCut);
        return newlineIndex >= 0 ? newlineIndex + 1 : roughCut;
    }
}
