using System.Net;
using VmixScheduler;

namespace VmixScheduler.Tests;

/// <summary>Simulates a completely unreachable Gumroad (offline venue wifi, DNS failure, etc.)
/// rather than returning a canned response.</summary>
internal sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("simulated network failure");
}

public class LicenseServiceTests
{
    private const string Fingerprint = "test-machine-fingerprint";

    private static string TempCachePath() => Path.Combine(Path.GetTempPath(), $"license-test-{Guid.NewGuid()}.dat");

    private static LicenseService MakeService(HttpMessageHandler handler, string cachePath, DateTime now) =>
        new(new GumroadLicenseClient(handler), new LicenseCache(cachePath), () => Fingerprint, () => now);

    private const string SuccessJson = """
        {"success": true, "message": null, "uses": 1, "purchase": {"email": "buyer@example.com", "refunded": false, "disputed": false, "chargebacked": false}}
        """;

    private const string RefundedJson = """
        {"success": true, "uses": 1, "purchase": {"email": "buyer@example.com", "refunded": true, "disputed": false, "chargebacked": false}}
        """;

    private const string DisputedJson = """
        {"success": true, "uses": 1, "purchase": {"email": "buyer@example.com", "refunded": false, "disputed": true, "chargebacked": false}}
        """;

    private const string NotFoundJson = """
        {"success": false, "message": "That license does not exist for the provided product."}
        """;

    [Fact]
    public async Task ActivateAsync_Succeeds_AndSavesCache()
    {
        var cachePath = TempCachePath();
        try
        {
            var handler = new FakeHttpMessageHandler(_ => SuccessJson);
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var service = MakeService(handler, cachePath, now);

            var result = await service.ActivateAsync("some-license-key");

            Assert.True(result.Success);

            var cache = new LicenseCache(cachePath).Load();
            Assert.NotNull(cache);
            Assert.Equal("some-license-key", cache!.LicenseKey);
            Assert.Equal("buyer@example.com", cache.Email);
            Assert.Equal(Fingerprint, cache.MachineFingerprint);
            Assert.Equal(now, cache.LastVerifiedUtc);
        }
        finally { File.Delete(cachePath); }
    }

    [Fact]
    public async Task ActivateAsync_Rejects_WhenPurchaseRefunded()
    {
        var cachePath = TempCachePath();
        try
        {
            var handler = new FakeHttpMessageHandler(_ => RefundedJson);
            var service = MakeService(handler, cachePath, DateTime.UtcNow);

            var result = await service.ActivateAsync("some-license-key");

            Assert.False(result.Success);
            Assert.Contains("refunded", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Null(new LicenseCache(cachePath).Load()); // rejected activation must not be cached
        }
        finally { File.Delete(cachePath); }
    }

    [Fact]
    public async Task ActivateAsync_Rejects_WhenPurchaseDisputed()
    {
        var cachePath = TempCachePath();
        try
        {
            var handler = new FakeHttpMessageHandler(_ => DisputedJson);
            var service = MakeService(handler, cachePath, DateTime.UtcNow);

            var result = await service.ActivateAsync("some-license-key");

            Assert.False(result.Success);
            Assert.Contains("dispute", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Null(new LicenseCache(cachePath).Load());
        }
        finally { File.Delete(cachePath); }
    }

    [Fact]
    public async Task ActivateAsync_Rejects_WhenLicenseNotRecognized()
    {
        var cachePath = TempCachePath();
        try
        {
            var handler = new FakeHttpMessageHandler(_ => NotFoundJson);
            var service = MakeService(handler, cachePath, DateTime.UtcNow);

            var result = await service.ActivateAsync("bogus-key");

            Assert.False(result.Success);
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        }
        finally { File.Delete(cachePath); }
    }

    [Fact]
    public void IsConfigured_False_WhilePermalinkIsStillThePlaceholder()
    {
        // Program.cs relies on this to skip the activation gate entirely until a real Gumroad
        // product exists — otherwise every install locks out behind a dialog that can never
        // succeed. Every constructor path bakes in the same placeholder permalink today, so this
        // is necessarily false regardless of which ctor/fakes are used.
        var service = MakeService(new FakeHttpMessageHandler(_ => SuccessJson), TempCachePath(), DateTime.UtcNow);

        Assert.False(service.IsConfigured);
    }

    [Fact]
    public async Task IsLicensedAsync_False_WhenNoCacheExists()
    {
        var cachePath = TempCachePath(); // never written to
        var handler = new FakeHttpMessageHandler(_ => SuccessJson);
        var service = MakeService(handler, cachePath, DateTime.UtcNow);

        Assert.False(await service.IsLicensedAsync());
    }

    [Fact]
    public async Task IsLicensedAsync_False_WhenCacheFingerprintDoesNotMatchThisMachine()
    {
        var cachePath = TempCachePath();
        try
        {
            var now = DateTime.UtcNow;
            new LicenseCache(cachePath).Save(new LicenseCacheData
            {
                LicenseKey = "some-license-key",
                MachineFingerprint = "a-different-machine-entirely",
                LastVerifiedUtc = now
            });

            var handler = new FakeHttpMessageHandler(_ => SuccessJson);
            var service = MakeService(handler, cachePath, now);

            Assert.False(await service.IsLicensedAsync());
            // A mismatched fingerprint should be rejected before ever calling out to Gumroad.
            Assert.Empty(handler.RequestedUrls);
        }
        finally { File.Delete(cachePath); }
    }

    [Fact]
    public async Task IsLicensedAsync_True_WhenRecentlyVerified_WithoutReVerifying()
    {
        var cachePath = TempCachePath();
        try
        {
            var verifiedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            new LicenseCache(cachePath).Save(new LicenseCacheData
            {
                LicenseKey = "some-license-key",
                MachineFingerprint = Fingerprint,
                LastVerifiedUtc = verifiedAt
            });

            var handler = new FakeHttpMessageHandler(_ => SuccessJson);
            var now = verifiedAt.AddDays(3); // well inside the 7-day reverify window
            var service = MakeService(handler, cachePath, now);

            Assert.True(await service.IsLicensedAsync());
            Assert.Empty(handler.RequestedUrls); // trusted the cache, no network call needed
        }
        finally { File.Delete(cachePath); }
    }

    [Fact]
    public async Task IsLicensedAsync_StaleAndOnline_ReVerifiesAndRefreshesCache()
    {
        var cachePath = TempCachePath();
        try
        {
            var verifiedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            new LicenseCache(cachePath).Save(new LicenseCacheData
            {
                LicenseKey = "some-license-key",
                MachineFingerprint = Fingerprint,
                LastVerifiedUtc = verifiedAt
            });

            var handler = new FakeHttpMessageHandler(_ => SuccessJson);
            var now = verifiedAt.AddDays(10); // past the 7-day reverify window
            var service = MakeService(handler, cachePath, now);

            Assert.True(await service.IsLicensedAsync());
            Assert.Single(handler.RequestedUrls); // did reverify over the network

            var refreshed = new LicenseCache(cachePath).Load();
            Assert.Equal(now, refreshed!.LastVerifiedUtc);
        }
        finally { File.Delete(cachePath); }
    }

    [Fact]
    public async Task IsLicensedAsync_StaleAndOffline_WithinGracePeriod_TrustsCache()
    {
        var cachePath = TempCachePath();
        try
        {
            var verifiedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            new LicenseCache(cachePath).Save(new LicenseCacheData
            {
                LicenseKey = "some-license-key",
                MachineFingerprint = Fingerprint,
                LastVerifiedUtc = verifiedAt
            });

            var handler = new ThrowingHttpMessageHandler(); // simulated offline venue wifi
            var now = verifiedAt.AddDays(10); // stale (past 7 days) but within the 14-day grace period
            var service = MakeService(handler, cachePath, now);

            Assert.True(await service.IsLicensedAsync());
        }
        finally { File.Delete(cachePath); }
    }

    [Fact]
    public async Task IsLicensedAsync_StaleAndOffline_BeyondGracePeriod_ReturnsFalse()
    {
        var cachePath = TempCachePath();
        try
        {
            var verifiedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            new LicenseCache(cachePath).Save(new LicenseCacheData
            {
                LicenseKey = "some-license-key",
                MachineFingerprint = Fingerprint,
                LastVerifiedUtc = verifiedAt
            });

            var handler = new ThrowingHttpMessageHandler();
            var now = verifiedAt.AddDays(20); // past the 14-day offline grace period
            var service = MakeService(handler, cachePath, now);

            Assert.False(await service.IsLicensedAsync());
        }
        finally { File.Delete(cachePath); }
    }
}
