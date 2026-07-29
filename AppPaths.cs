namespace VmixScheduler;

/// <summary>
/// Where this app writes its own durable files (crash.log, as-run logs). Deliberately NOT
/// AppContext.BaseDirectory (the install folder) — a normal, non-elevated launch of an app
/// installed under Program Files cannot write there, so anything logged that way silently
/// never actually gets written. ProgramData is writable by a standard user and shared across
/// whoever's logged in on this machine, which also matches how a single-vMix station actually
/// operates (one machine, one show, everyone should see the same logs).
/// </summary>
public static class AppPaths
{
    public static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VmixScheduler");

    public static readonly string AsRunLogDirectory = Path.Combine(DataDirectory, "AsRunLogs");
}
