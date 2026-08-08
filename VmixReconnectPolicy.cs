namespace VmixScheduler;

/// <summary>
/// Decides whether regaining contact with vMix should re-run the ad-overlay restore path
/// (RestoreAdOverlaysAsync in Form1). Extracted as a pure function so the decision is
/// unit-testable: Form1 previously gated the restore on a "_hasSyncedOnce" flag that only ever
/// went true once per app lifetime, so if vMix itself restarted or hung mid-session — the app
/// surviving throughout — the restore path would never fire again for the rest of that run. If
/// overlays 1/3/4 happened to be in their normal "on" state (not suppressed for an ad) at the
/// moment vMix dropped, they'd come back from the vMix-side restart already off, and nothing
/// would ever turn them back on until an ad break happened to fire and end (which could be hours,
/// or never, depending on the ad schedule) — a broadcast left running for days would show that gap
/// as overlays stuck off, or overlay text stuck stale, indefinitely. The fix: track connectivity
/// itself (not "have we ever synced"), so any reconnect — including the app's very first sync —
/// re-checks and restores overlay state.
/// </summary>
public static class VmixReconnectPolicy
{
    /// <summary>
    /// True when a just-regained connection to vMix should re-run the overlay restore path: we were
    /// previously unable to reach vMix (or have never yet reached it), and overlays aren't currently
    /// suppressed for an ad (an ad ending on its own already triggers a restore via the normal
    /// ad-state transition, so this only needs to cover the "overlays were supposed to be on" case).
    /// </summary>
    public static bool ShouldRestoreOverlaysOnReconnect(bool wasUnreachable, bool adOverlaysCurrentlySuppressed) =>
        wasUnreachable && !adOverlaysCurrentlySuppressed;
}
