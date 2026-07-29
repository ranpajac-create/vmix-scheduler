namespace VmixScheduler;

static class Program
{
    // Same AppId as installer/VmixScheduler.iss — identifies "this app" regardless of which copy
    // (installed build, a dev build, an old version) is launching, so a second copy started from
    // anywhere on this machine is still caught. A machine only ever has one vMix to control; two
    // schedulers racing to control it at once causes duplicate automation (double ad cuts,
    // conflicting overlay triggers), which is what this guards against.
    private const string SingleInstanceMutexName = "VmixScheduler-{AF5D0A65-8273-4C20-8BD2-F158C41B618E}";

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        using var singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Another vMix Scheduler instance is already running on this machine and may be " +
                "actively controlling vMix.\n\nRunning two instances at once can cause duplicate " +
                "automation — double ad cuts, conflicting overlay triggers, and similar glitches. " +
                "Close the other instance first.",
                "vMix Scheduler Already Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // This drives live broadcast output unattended — an unhandled exception anywhere should
        // be logged rather than silently killing the app mid-show.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) => LogCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => LogCrash(e.ExceptionObject as Exception);

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }

    private static void LogCrash(Exception? ex)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { /* best effort */ }
    }
}