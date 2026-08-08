using Microsoft.Win32;

namespace VmixScheduler;

public class LicenseActivationResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }

    private LicenseActivationResult(bool success, string? errorMessage)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }

    public static LicenseActivationResult Ok() => new(true, null);
    public static LicenseActivationResult Fail(string message) => new(false, message);
}

/// <summary>Ties a license activation to this specific machine so a copied cache file doesn't
/// just work if handed to someone else.</summary>
public static class MachineFingerprint
{
    public static string Get()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", null) as string;
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        catch
        {
            // Registry read can fail under odd permission setups — fall back below rather than crash.
        }

        return Environment.MachineName;
    }
}

/// <summary>
/// Orchestrates Gumroad license activation/verification. This app runs unattended during live
/// broadcasts, so once activated it must not go dark just because a venue's wifi drops mid-show —
/// see the offline grace period in IsLicensedAsync.
/// </summary>
public class LicenseService
{
    // TODO: swap in the real Gumroad product permalink once the product is set up.
    private const string ProductPermalink = "your-product-permalink-here";
    private const string UnconfiguredPermalink = "your-product-permalink-here";

    /// <summary>
    /// False until a real Gumroad product permalink replaces the placeholder above. Program.cs
    /// checks this before enforcing activation at all — without it, shipping this gate ahead of
    /// the actual Gumroad product being set up would permanently lock every install out behind an
    /// activation dialog that can never succeed (there's nothing real for it to verify against),
    /// which is exactly what happened the first time this went out.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ProductPermalink) &&
        !string.Equals(ProductPermalink, UnconfiguredPermalink, StringComparison.Ordinal);

    private static readonly TimeSpan ReverifyInterval = TimeSpan.FromDays(7);
    private static readonly TimeSpan OfflineGracePeriod = TimeSpan.FromDays(14);

    private readonly GumroadLicenseClient _client;
    private readonly LicenseCache _cache;
    private readonly Func<string> _fingerprintProvider;
    private readonly Func<DateTime> _now;

    public LicenseService() : this(new GumroadLicenseClient(), new LicenseCache(), MachineFingerprint.Get, () => DateTime.UtcNow) { }

    /// <summary>Lets tests inject a fake Gumroad transport (via GumroadLicenseClient's own fake-handler
    /// constructor), an isolated cache file, a fixed machine fingerprint, and a controllable clock —
    /// instead of touching the real network, disk, registry, and wall clock.</summary>
    public LicenseService(GumroadLicenseClient client, LicenseCache cache, Func<string> fingerprintProvider, Func<DateTime> now)
    {
        _client = client;
        _cache = cache;
        _fingerprintProvider = fingerprintProvider;
        _now = now;
    }

    public async Task<LicenseActivationResult> ActivateAsync(string licenseKey)
    {
        GumroadVerifyResult result;
        try
        {
            result = await _client.VerifyAsync(ProductPermalink, licenseKey, incrementUsesCount: true);
        }
        catch (Exception ex)
        {
            return LicenseActivationResult.Fail($"Couldn't reach Gumroad to activate — {ex.Message}");
        }

        if (!result.Success)
            return LicenseActivationResult.Fail(result.Message ?? "License key not recognized.");

        var rejection = RejectionReason(result.Purchase);
        if (rejection != null)
            return LicenseActivationResult.Fail(rejection);

        _cache.Save(new LicenseCacheData
        {
            LicenseKey = licenseKey,
            Email = result.Purchase?.Email,
            MachineFingerprint = _fingerprintProvider(),
            LastVerifiedUtc = _now()
        });

        return LicenseActivationResult.Ok();
    }

    public async Task<bool> IsLicensedAsync()
    {
        var cached = _cache.Load();
        if (cached == null) return false;

        // A cache file copied onto a different PC (or handed to someone else) has the wrong
        // fingerprint baked in and is rejected outright, regardless of age.
        if (!string.Equals(cached.MachineFingerprint, _fingerprintProvider(), StringComparison.Ordinal))
            return false;

        var age = _now() - cached.LastVerifiedUtc;
        if (age < ReverifyInterval) return true;

        try
        {
            var result = await _client.VerifyAsync(ProductPermalink, cached.LicenseKey, incrementUsesCount: false);
            if (!result.Success) return false;
            if (RejectionReason(result.Purchase) != null) return false;

            _cache.Save(new LicenseCacheData
            {
                LicenseKey = cached.LicenseKey,
                Email = result.Purchase?.Email ?? cached.Email,
                MachineFingerprint = cached.MachineFingerprint,
                LastVerifiedUtc = _now()
            });
            return true;
        }
        catch
        {
            // Offline (or Gumroad unreachable) — trust the last successful verification for a
            // longer grace window rather than locking out a live broadcast over a network blip.
            return age < OfflineGracePeriod;
        }
    }

    private static string? RejectionReason(GumroadPurchase? purchase)
    {
        if (purchase == null) return null;
        if (purchase.Refunded) return "This license was refunded and can no longer be used.";
        if (purchase.Disputed) return "This license is under a payment dispute and can no longer be used.";
        if (purchase.Chargebacked) return "This license was charged back and can no longer be used.";
        return null;
    }
}
