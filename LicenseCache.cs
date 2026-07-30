using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VmixScheduler;

public class LicenseCacheData
{
    public string LicenseKey { get; set; } = "";
    public string? Email { get; set; }
    public string MachineFingerprint { get; set; } = "";
    public DateTime LastVerifiedUtc { get; set; }
}

/// <summary>
/// Persists license activation state to a file under AppPaths.DataDirectory (the same
/// %ProgramData%\VmixScheduler\ folder the as-run logs and crash.log already use), encrypted with
/// Windows DPAPI (CurrentUser scope) so it isn't a plain-text file someone can hand-edit to fake
/// activation, and won't decrypt at all if copied to a different Windows account.
/// </summary>
public class LicenseCache
{
    private readonly string _filePath;

    public LicenseCache() : this(Path.Combine(AppPaths.DataDirectory, "license.dat")) { }

    /// <summary>Lets tests point at an isolated temp file instead of the real cache location.</summary>
    public LicenseCache(string filePath)
    {
        _filePath = filePath;
    }

    public void Save(LicenseCacheData data)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(data);
        var plainBytes = Encoding.UTF8.GetBytes(json);
        var protectedBytes = ProtectedData.Protect(plainBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, protectedBytes);
    }

    /// <summary>Returns null if there's no cache file, or if it can't be read/decrypted/parsed —
    /// tampered, corrupt, or protected under a different Windows account are all treated as "not
    /// activated" rather than throwing.</summary>
    public LicenseCacheData? Load()
    {
        if (!File.Exists(_filePath)) return null;
        try
        {
            var protectedBytes = File.ReadAllBytes(_filePath);
            var plainBytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(plainBytes);
            return JsonSerializer.Deserialize<LicenseCacheData>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Clear()
    {
        try { File.Delete(_filePath); } catch { /* best effort */ }
    }
}
