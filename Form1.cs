namespace VmixScheduler;

public partial class Form1 : Form
{
    private static readonly string[] RoleNames =
        { "Filler", "Now", "Next", "NowSong", "NextSong", "Backin", "Overlay1", "Overlay2", "Overlay3", "Overlay4" };
    private static readonly int[] AutomatedOverlayChannels = { 1, 2, 3, 4 };
    private static readonly int[] StaticOverlayChannels = { 1, 3, 4 }; // fixed-content overlays restored with a pinned input

    private const int Overlay2PopupOffsetMs = 10_000; // trigger points: 10s after start / 10s before end
    private const int Overlay2PopupDurationMs = 8_000; // how long each graphic stays visible

    private readonly VmixClient _client;
    private readonly Dictionary<string, VmixInput> _roleInputs = new(StringComparer.OrdinalIgnoreCase);
    private List<ScheduleRule> _rules = new();

    private DateTime _fillerCooldownUntil = DateTime.MinValue;
    private DateTime _lastAutomationErrorLog = DateTime.MinValue;

    // Tracks "are overlays currently suppressed for an ad" purely from what's on Program right
    // now — so it reacts the same way whether the ad was fired by this app's schedule or the
    // operator switched to it manually in vMix.
    private bool _adOverlaysOff;

    // What was on Program the tick before the current one — captured into _preAdInputKey the
    // moment an ad break starts, so HandleAutoFillerAsync can cut back to that program instead
    // of the filler once the ad(s) finish.
    private string? _previousActiveKey;
    private string? _preAdInputKey;

    private string? _overlay2ItemKey;
    private readonly HashSet<string> _overlay2FiredForItem = new();
    private bool _overlay2Visible;
    private DateTime _overlay2HideAt = DateTime.MinValue;

    private const int AutoSyncIntervalSeconds = 15;
    private bool _isSyncing;
    private DateTime _lastAutoSync = DateTime.MinValue;
    private bool _hasSyncedOnce;
    private bool _isTickRunning;
    private DateTime _lastNowNextUpdate = DateTime.MinValue;

    public Form1()
    {
        InitializeComponent();
        _client = new VmixClient();
        tmrCheck.Start();
        Log("vMix Scheduler started. Rename vMix inputs per the naming convention — syncing automatically.");
        _ = SyncFromVmixAsync(silent: false);
    }

    private int GetPort() => int.TryParse(txtPort.Text.Trim(), out var p) ? p : 8088;

    private int GetNowNextIntervalSeconds() => cmbNowNextInterval.SelectedItem?.ToString() switch
    {
        "1s" => 1,
        "2s" => 2,
        "5s" => 5,
        "10s" => 10,
        "30s" => 30,
        "1 min" => 60,
        "10 min" => 600,
        _ => 5,
    };

    private void Log(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void LogThrottled(string message, DateTime now)
    {
        if ((now - _lastAutomationErrorLog).TotalSeconds < 30) return;
        _lastAutomationErrorLog = now;
        Log(message);
    }

    // ---------- Refresh & sync ----------

    private async void btnRefreshInputs_Click(object? sender, EventArgs e)
    {
        await SyncFromVmixAsync(silent: false);
    }

    /// <summary>
    /// Pulls inputs from vMix and re-detects roles/rules. Called on startup, every
    /// AutoSyncIntervalSeconds from the tick, and on demand from the Refresh Inputs button —
    /// so newly renamed inputs are picked up without the user having to remember to click anything.
    /// </summary>
    private async Task SyncFromVmixAsync(bool silent)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        btnRefreshInputs.Enabled = false;
        lblConnectionStatus.Text = "Connecting...";
        lblConnectionStatus.ForeColor = Color.DimGray;
        var host = txtHost.Text.Trim();
        var port = GetPort();
        try
        {
            var inputs = await _client.GetInputsAsync(host, port);
            SyncRoles(inputs);
            SyncRules(inputs);
            RefreshGrid();
            if (!_hasSyncedOnce && !_adOverlaysOff)
                await RestoreAdOverlaysAsync(host, port, "Startup sync");
            _hasSyncedOnce = true;
            lblConnectionStatus.Text = $"Connected — {inputs.Count} input(s), {_rules.Count} rule(s)";
            lblConnectionStatus.ForeColor = Color.SeaGreen;
            if (!silent)
                Log($"Synced from {host}:{port} — {inputs.Count} inputs, {_rules.Count} schedule rule(s) detected.");
        }
        catch (Exception ex)
        {
            lblConnectionStatus.Text = "Connection failed";
            lblConnectionStatus.ForeColor = Color.Firebrick;
            Log($"Failed to connect to vMix at {host}:{port} — {ex.Message}");
        }
        finally
        {
            btnRefreshInputs.Enabled = true;
            _isSyncing = false;
            _lastAutoSync = DateTime.Now;
        }
    }

    private void SyncRoles(List<VmixInput> inputs)
    {
        _roleInputs.Clear();
        foreach (var role in RoleNames)
        {
            var match = inputs.FirstOrDefault(i => string.Equals(i.Name.Trim(), role, StringComparison.OrdinalIgnoreCase));
            if (match != null) _roleInputs[role] = match;
        }

        SetRoleLabel(lblRoleFillerValue, "Filler");
        SetRoleLabel(lblRoleNowValue, "Now");
        SetRoleLabel(lblRoleNextValue, "Next");
        SetRoleLabel(lblRoleNowSongValue, "NowSong");
        SetRoleLabel(lblRoleNextSongValue, "NextSong");
        SetRoleLabel(lblRoleBackinValue, "Backin");
        SetRoleLabel(lblRoleOverlay1Value, "Overlay1");
        SetRoleLabel(lblRoleOverlay2Value, "Overlay2");
        SetRoleLabel(lblRoleOverlay3Value, "Overlay3");
        SetRoleLabel(lblRoleOverlay4Value, "Overlay4");
    }

    private void SetRoleLabel(Label label, string role)
    {
        if (_roleInputs.TryGetValue(role, out var input))
        {
            label.Text = input.Name;
            label.ForeColor = Color.SeaGreen;
        }
        else
        {
            label.Text = "(not found)";
            label.ForeColor = Color.DimGray;
        }
    }

    private void SyncRules(List<VmixInput> inputs)
    {
        var previous = _rules;
        var updated = new List<ScheduleRule>();
        foreach (var input in inputs)
        {
            var rule = ScheduleRuleParser.Parse(input);
            if (rule == null) continue;

            var prior = previous.FirstOrDefault(r => r.InputKey == rule.InputKey && r.RawTitle == rule.RawTitle);
            if (prior != null) rule.LastFiredOccurrence = prior.LastFiredOccurrence;

            updated.Add(rule);
        }
        _rules = updated;
    }

    private void RefreshGrid()
    {
        var now = DateTime.Now;
        dgvSchedule.Rows.Clear();
        foreach (var rule in _rules.OrderBy(r => r.ComputeNextOccurrence(now) ?? DateTime.MaxValue))
        {
            var next = rule.ComputeNextOccurrence(now);
            var status = rule.LastFiredOccurrence.HasValue
                ? $"Last fired {rule.LastFiredOccurrence:yyyy-MM-dd HH:mm:ss}"
                : "Not yet fired";

            int rowIndex = dgvSchedule.Rows.Add(
                rule.RawTitle,
                rule.DisplayName,
                rule.Category.ToString(),
                rule.RecurrenceDisplay,
                next?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-",
                status);
            dgvSchedule.Rows[rowIndex].Tag = rule;
        }
    }

    private async void btnTriggerSelected_Click(object? sender, EventArgs e)
    {
        if (dgvSchedule.SelectedRows.Count == 0) return;
        if (dgvSchedule.SelectedRows[0].Tag is not ScheduleRule rule) return;

        var host = txtHost.Text.Trim();
        var port = GetPort();
        try
        {
            await FireRuleAsync(host, port, rule, DateTime.Now);
            Log($"Manually triggered '{rule.DisplayName}'.");
            RefreshGrid();
        }
        catch (Exception ex)
        {
            Log($"FAILED to manually trigger '{rule.DisplayName}' — {ex.Message}");
        }
    }

    // ---------- Firing rules ----------

    private async Task FireRuleAsync(string host, int port, ScheduleRule rule, DateTime occurrence)
    {
        await _client.TriggerInputAsync(host, port, rule.InputKey);
        rule.LastFiredOccurrence = occurrence;

        // Suppress immediately so there's no gap between the cut and the overlays dropping;
        // HandleAdOverlayStateAsync (driven off what's actually on Program) keeps this in sync
        // afterward, including if the operator switches ads manually from here on.
        if (rule.Category is ScheduleCategory.Ad or ScheduleCategory.LShapeAd && !_adOverlaysOff)
        {
            // Do this here too (not just in HandleAdOverlayStateAsync) — this path sets
            // _adOverlaysOff itself, so by the time HandleAdOverlayStateAsync sees the ad later
            // in the same tick, its own "!_adOverlaysOff" capture check would already be false.
            _preAdInputKey = _previousActiveKey;
            await SuppressAdOverlaysAsync(host, port);
            _adOverlaysOff = true;
        }
    }

    private async Task SuppressAdOverlaysAsync(string host, int port)
    {
        // Off doesn't need a target input, so channel 2 (dynamic) suppresses the same way as 3/4 (static).
        foreach (var channel in AutomatedOverlayChannels)
        {
            try { await _client.OverlayOffAsync(host, port, channel); }
            catch (Exception ex) { Log($"Overlay{channel} off failed — {ex.Message}"); }
        }
        _overlay2Visible = false;
        Log("Ad break started — overlays 1-4 suppressed.");
    }

    private async Task RestoreAdOverlaysAsync(string host, int port, string reason = "Ad break ended")
    {
        bool any = false;
        foreach (var channel in StaticOverlayChannels)
        {
            if (!_roleInputs.TryGetValue($"Overlay{channel}", out var overlayInput)) continue;
            any = true;
            try { await _client.OverlayOnAsync(host, port, channel, overlayInput.Key); }
            catch (Exception ex) { Log($"Overlay{channel} restore failed — {ex.Message}"); }
        }
        if (any) Log($"{reason} — overlays 1,3,4 confirmed on.");
        // Overlay2 is dynamic (Now/Next/NowSong/NextSong cycling) — left off here; UpdateOverlay2Async
        // brings the right graphic back at its next trigger point instead of pinning a fixed input.
    }

    // ---------- Main tick ----------

    private async void tmrCheck_Tick(object? sender, EventArgs e)
    {
        if (_isTickRunning) return;
        _isTickRunning = true;
        try
        {
            var host = txtHost.Text.Trim();
            var port = GetPort();
            var now = DateTime.Now;

            if ((now - _lastAutoSync).TotalSeconds >= AutoSyncIntervalSeconds)
                await SyncFromVmixAsync(silent: true);

            var due = _rules.Where(r =>
            {
                var occ = r.ComputeOccurrence(now);
                return occ.HasValue && occ.Value <= now && (now - occ.Value) <= TimeSpan.FromSeconds(3) && r.LastFiredOccurrence != occ.Value;
            }).ToList();

            foreach (var rule in due)
            {
                var occurrence = rule.ComputeOccurrence(now)!.Value;
                try
                {
                    await FireRuleAsync(host, port, rule, occurrence);
                    Log($"Triggered '{rule.DisplayName}' ({rule.Category}, {rule.RecurrenceDisplay}).");
                }
                catch (Exception ex)
                {
                    Log($"FAILED to trigger '{rule.DisplayName}' — {ex.Message}");
                }
            }
            if (due.Count > 0) RefreshGrid();

            if (_rules.Count == 0 && _roleInputs.Count == 0) return;

            VmixStatus status;
            try
            {
                status = await _client.GetStatusAsync(host, port);
            }
            catch (Exception ex)
            {
                LogThrottled($"Live automation: failed to reach vMix — {ex.Message}", now);
                return;
            }

            var active = status.FindActive();
            var fieldName = string.IsNullOrWhiteSpace(txtFieldName.Text) ? "Headline.Text" : txtFieldName.Text.Trim();

            if ((now - _lastNowNextUpdate).TotalSeconds >= GetNowNextIntervalSeconds())
            {
                await UpdateNowNextAsync(host, port, active, fieldName, now);
                _lastNowNextUpdate = now;
            }
            await UpdateNowNextSongAsync(host, port, active, fieldName, now);
            await UpdateBackinAsync(host, port, active, fieldName, now);
            await HandleAdOverlayStateAsync(host, port, active);
            await HandleAutoFillerAsync(host, port, active, now);

            if (!_adOverlaysOff)
                await UpdateOverlay2Async(host, port, active, now);

            _previousActiveKey = active?.Key;
        }
        catch (Exception ex)
        {
            // The automation tick drives live output every second — an unhandled exception here
            // would propagate out of this async void handler and take the whole app down mid-show.
            // Log and keep ticking instead of crashing.
            Log($"Unexpected error during automation tick — {ex.Message}");
        }
        finally
        {
            _isTickRunning = false;
        }
    }

    private async Task UpdateNowNextAsync(string host, int port, VmixInput? active, string fieldName, DateTime now)
    {
        var nowText = BestDisplayText(active);
        var nextText = active?.NextSongTitle ?? "";

        if (_roleInputs.TryGetValue("Now", out var nowInput))
            await TrySetText(host, port, nowInput.Key, fieldName, nowText, now);

        if (_roleInputs.TryGetValue("Next", out var nextInput))
            await TrySetText(host, port, nextInput.Key, fieldName, nextText, now);
    }

    /// <summary>
    /// Keeps NowSong/NextSong tracking the actively-playing Filler list item every tick —
    /// unlike UpdateNowNextAsync, this must not be throttled by the Now/Next interval, since
    /// songs can change far more often than that interval.
    /// </summary>
    private async Task UpdateNowNextSongAsync(string host, int port, VmixInput? active, string fieldName, DateTime now)
    {
        bool isFillerActive = active != null && _roleInputs.TryGetValue("Filler", out var fillerForSong) && active.Number == fillerForSong.Number;
        if (isFillerActive)
        {
            if (_roleInputs.TryGetValue("NowSong", out var nowSongInput))
                await TrySetText(host, port, nowSongInput.Key, fieldName, active!.CurrentSongTitle ?? "", now);

            if (_roleInputs.TryGetValue("NextSong", out var nextSongInput))
                await TrySetText(host, port, nextSongInput.Key, fieldName, active!.NextSongTitle ?? "", now);
        }
    }

    /// <summary>
    /// Picks what to show on a Now/Next graphic: only ever the actual media file name — the
    /// playing list item for list inputs (e.g. Filler), else the underlying file (vMix's raw
    /// title, unaffected by the schedule-code rename). Input/schedule names are never shown as a
    /// substitute — if vMix has no file name left to give (e.g. a renamed single-file input with
    /// no list to fall back on), this returns empty rather than the input's rename.
    /// </summary>
    private static string BestDisplayText(VmixInput? input)
    {
        if (!string.IsNullOrEmpty(input?.CurrentSongTitle)) return input!.CurrentSongTitle!;
        if (!string.IsNullOrEmpty(input?.FileName)) return input!.FileName!;
        return "";
    }

    /// <summary>
    /// Overlay2 is a shared "ticker" channel: it cycles Now/Next graphics while a scheduled
    /// Program is on air, and NowSong/NextSong graphics while the Filler is on air, popping up
    /// at start (+10s), mid-point, and end (-10s) of the current item and hiding after a fixed duration.
    /// </summary>
    private async Task UpdateOverlay2Async(string host, int port, VmixInput? active, DateTime now)
    {
        if (active == null || active.Duration <= 0)
        {
            await HideOverlay2IfVisible(host, port);
            return;
        }

        var activeRule = _rules.FirstOrDefault(r => r.InputKey == active.Key);
        bool isProgram = activeRule?.Category == ScheduleCategory.Program;
        bool isFiller = _roleInputs.TryGetValue("Filler", out var filler) && active.Number == filler.Number;

        if (!isProgram && !isFiller)
        {
            await HideOverlay2IfVisible(host, port);
            return;
        }

        var itemKey = isFiller ? $"{active.Key}|{active.CurrentSongTitle}" : active.Key;
        if (itemKey != _overlay2ItemKey)
        {
            _overlay2ItemKey = itemKey;
            _overlay2FiredForItem.Clear();
            _overlay2Visible = false;
        }

        // Let whatever's currently showing finish its full visible window before considering
        // the next trigger — otherwise, if several thresholds are already satisfied at once
        // (e.g. right after the app restarts mid-clip), they'd replace each other every tick.
        if (_overlay2Visible)
        {
            if (now < _overlay2HideAt) return;
            await HideOverlay2IfVisible(host, port);
        }

        var elapsed = active.Position;
        var remaining = active.Duration - active.Position;
        var midpoint = active.Duration / 2;

        var startRole = isProgram ? "Now" : "NowSong";
        var midRole = isProgram ? "Next" : "NextSong";

        string? toShow = null;
        if (elapsed >= Overlay2PopupOffsetMs && !_overlay2FiredForItem.Contains("start"))
        {
            toShow = startRole;
            _overlay2FiredForItem.Add("start");
        }
        else if (elapsed >= midpoint && !_overlay2FiredForItem.Contains("mid"))
        {
            toShow = midRole;
            _overlay2FiredForItem.Add("mid");
        }
        else if (remaining <= Overlay2PopupOffsetMs && !_overlay2FiredForItem.Contains("end"))
        {
            toShow = midRole;
            _overlay2FiredForItem.Add("end");
        }

        if (toShow != null && _roleInputs.TryGetValue(toShow, out var showInput))
        {
            try
            {
                await _client.OverlayOnAsync(host, port, 2, showInput.Key);
                _overlay2Visible = true;
                _overlay2HideAt = now.AddMilliseconds(Overlay2PopupDurationMs);
                Log($"Overlay2: showing '{toShow}'.");
            }
            catch (Exception ex)
            {
                LogThrottled($"Overlay2 show failed — {ex.Message}", now);
            }
        }
    }

    private async Task HideOverlay2IfVisible(string host, int port)
    {
        if (!_overlay2Visible) return;
        try { await _client.OverlayOffAsync(host, port, 2); }
        catch { /* best effort */ }
        _overlay2Visible = false;
    }

    private async Task UpdateBackinAsync(string host, int port, VmixInput? active, string fieldName, DateTime now)
    {
        if (!_roleInputs.TryGetValue("Backin", out var backinInput)) return;

        var text = "--:--";
        if (active != null && active.Duration > 0)
        {
            var remainingMs = Math.Max(0, active.Duration - active.Position);
            text = TimeSpan.FromMilliseconds(remainingMs).ToString(@"mm\:ss");
        }
        await TrySetText(host, port, backinInput.Key, fieldName, text, now);
    }

    /// <summary>
    /// Keeps overlay suppression in sync with whatever is actually on Program, so it reacts the
    /// same way whether an Ad/L-shape ad rule was fired by this app or the operator cut to that
    /// input manually in vMix.
    /// </summary>
    private async Task HandleAdOverlayStateAsync(string host, int port, VmixInput? active)
    {
        var activeRule = active != null ? _rules.FirstOrDefault(r => r.InputKey == active.Key) : null;
        bool adOnAir = activeRule?.Category is ScheduleCategory.Ad or ScheduleCategory.LShapeAd;

        if (adOnAir && !_adOverlaysOff)
        {
            // Entering an ad break (or the first ad of a back-to-back pod) — remember what was
            // on Program so HandleAutoFillerAsync can return to it once the ad(s) finish.
            _preAdInputKey = _previousActiveKey;
            await SuppressAdOverlaysAsync(host, port);
            _adOverlaysOff = true;
        }
        else if (!adOnAir && _adOverlaysOff)
        {
            await RestoreAdOverlaysAsync(host, port);
            _adOverlaysOff = false;
        }
    }

    private async Task HandleAutoFillerAsync(string host, int port, VmixInput? active, DateTime now)
    {
        if (!_roleInputs.TryGetValue("Filler", out var filler)) return;
        if (active == null || active.Duration <= 0) return;
        if (active.Position < active.Duration - 300) return;
        if (now < _fillerCooldownUntil) return;

        bool somethingDueSoon = _rules.Any(r =>
        {
            var next = r.ComputeNextOccurrence(now);
            return next.HasValue && next.Value <= now.AddSeconds(3);
        });
        if (somethingDueSoon) return;

        if (active.Number == filler.Number)
        {
            bool atLastItem = active.ListItems.Count == 0 || active.SelectedIndex >= active.ListItems.Count - 1;
            if (!atLastItem) return; // vMix is already mid-list; let it keep advancing on its own

            _fillerCooldownUntil = now.AddSeconds(5);
            try
            {
                await _client.LoopListToStartAsync(host, port, filler.Key);
                Log("Auto-filler: reached end of list, looping back to the first item.");
            }
            catch (Exception ex) { Log($"Auto-filler loop failed — {ex.Message}"); }
            return;
        }

        // If what's ending is an ad, go back to whatever program was on air before the ad break
        // instead of falling through to the filler — the filler is only for when there's truly
        // nothing else to show.
        var activeRule = _rules.FirstOrDefault(r => r.InputKey == active.Key);
        bool activeIsAd = activeRule?.Category is ScheduleCategory.Ad or ScheduleCategory.LShapeAd;
        if (activeIsAd && !string.IsNullOrEmpty(_preAdInputKey) && _preAdInputKey != active.Key)
        {
            var resumeKey = _preAdInputKey;
            // Guard against resuming straight into another ad (e.g. _preAdInputKey captured mid
            // ad-pod) — that would keep _adOverlaysOff stuck "true" forever since active would
            // never become non-ad again.
            var resumeRule = _rules.FirstOrDefault(r => r.InputKey == resumeKey);
            bool resumeIsAlsoAd = resumeRule?.Category is ScheduleCategory.Ad or ScheduleCategory.LShapeAd;
            if (!resumeIsAlsoAd)
            {
                _fillerCooldownUntil = now.AddSeconds(5);
                try
                {
                    await _client.ResumeInputAsync(host, port, resumeKey);
                    Log("Ad break ended, resuming previous program.");
                }
                catch (Exception ex) { Log($"Resuming previous program failed — {ex.Message}"); }
                return;
            }
        }

        _fillerCooldownUntil = now.AddSeconds(5);
        try
        {
            await _client.ResumeInputAsync(host, port, filler.Key);
            Log($"Auto-filler: '{active.Name}' ended, switched to filler '{filler.Name}'.");
        }
        catch (Exception ex) { Log($"Auto-filler trigger failed — {ex.Message}"); }
    }

    private async Task TrySetText(string host, int port, string inputKey, string fieldName, string value, DateTime now)
    {
        try
        {
            await _client.SetTextAsync(host, port, inputKey, fieldName, value);
        }
        catch (Exception ex)
        {
            LogThrottled($"Text overlay update failed (field '{fieldName}') — {ex.Message}", now);
        }
    }
}