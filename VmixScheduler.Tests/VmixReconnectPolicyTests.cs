using VmixScheduler;

namespace VmixScheduler.Tests;

public class VmixReconnectPolicyTests
{
    [Fact]
    public void ShouldRestore_WhenWasUnreachable_AndOverlaysNotSuppressedForAd()
    {
        // The core long-uptime scenario: vMix restarts mid-session while no ad is on air (the
        // common case), so overlays 1/3/4 come back from vMix off, but the app's own
        // _adOverlaysOff flag never transitioned — this must trigger a restore.
        Assert.True(VmixReconnectPolicy.ShouldRestoreOverlaysOnReconnect(wasUnreachable: true, adOverlaysCurrentlySuppressed: false));
    }

    [Fact]
    public void ShouldNotRestore_WhenAlreadyReachable()
    {
        // Steady-state ticks (the overwhelming majority over a multi-day run) must not re-fire
        // the restore path on every successful poll.
        Assert.False(VmixReconnectPolicy.ShouldRestoreOverlaysOnReconnect(wasUnreachable: false, adOverlaysCurrentlySuppressed: false));
        Assert.False(VmixReconnectPolicy.ShouldRestoreOverlaysOnReconnect(wasUnreachable: false, adOverlaysCurrentlySuppressed: true));
    }

    [Fact]
    public void ShouldNotRestore_WhenOverlaysCurrentlySuppressedForAd()
    {
        // If an ad happens to be on air right as vMix comes back, the normal ad-end transition
        // (HandleAdOverlayStateAsync) already owns restoring overlays once the ad finishes —
        // restoring here too would be redundant, not incorrect, but the policy intentionally
        // leaves that case to the existing path.
        Assert.False(VmixReconnectPolicy.ShouldRestoreOverlaysOnReconnect(wasUnreachable: true, adOverlaysCurrentlySuppressed: true));
    }
}
