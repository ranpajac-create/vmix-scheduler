using System.Text.Json;

namespace VmixScheduler;

public class GumroadPurchase
{
    public string? Email { get; set; }
    public bool Refunded { get; set; }
    public bool Disputed { get; set; }
    public bool Chargebacked { get; set; }
}

public class GumroadVerifyResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int Uses { get; set; }
    public GumroadPurchase? Purchase { get; set; }
}

/// <summary>Talks to Gumroad's license verification endpoint directly — see
/// https://help.gumroad.com/article/76-license-keys.</summary>
public class GumroadLicenseClient
{
    private const string VerifyUrl = "https://api.gumroad.com/v2/licenses/verify";

    private readonly HttpClient _http;

    public GumroadLicenseClient() : this(null) { }

    /// <summary>Lets tests inject a fake transport instead of hitting Gumroad's real API.</summary>
    public GumroadLicenseClient(HttpMessageHandler? handler)
    {
        _http = handler != null ? new HttpClient(handler) : new HttpClient();
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Gumroad responds with HTTP 404 (not just a 200 with success:false) for an unrecognized
    /// license key, so the body is parsed regardless of status code rather than calling
    /// EnsureSuccessStatusCode first.
    /// </summary>
    public async Task<GumroadVerifyResult> VerifyAsync(string productPermalink, string licenseKey, bool incrementUsesCount)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["product_permalink"] = productPermalink,
            ["license_key"] = licenseKey,
            ["increment_uses_count"] = incrementUsesCount ? "true" : "false"
        });

        using var response = await _http.PostAsync(VerifyUrl, content);
        var json = await response.Content.ReadAsStringAsync();
        return ParseResponse(json);
    }

    private static GumroadVerifyResult ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = new GumroadVerifyResult
        {
            Success = root.TryGetProperty("success", out var successEl) && successEl.ValueKind == JsonValueKind.True,
            Message = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null,
            Uses = root.TryGetProperty("uses", out var usesEl) && usesEl.ValueKind == JsonValueKind.Number ? usesEl.GetInt32() : 0
        };

        if (root.TryGetProperty("purchase", out var purchaseEl) && purchaseEl.ValueKind == JsonValueKind.Object)
        {
            result.Purchase = new GumroadPurchase
            {
                Email = purchaseEl.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null,
                Refunded = purchaseEl.TryGetProperty("refunded", out var refundedEl) && refundedEl.ValueKind == JsonValueKind.True,
                Disputed = purchaseEl.TryGetProperty("disputed", out var disputedEl) && disputedEl.ValueKind == JsonValueKind.True,
                Chargebacked = purchaseEl.TryGetProperty("chargebacked", out var chargebackedEl) && chargebackedEl.ValueKind == JsonValueKind.True
            };
        }

        return result;
    }
}
