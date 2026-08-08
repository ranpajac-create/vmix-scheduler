namespace VmixScheduler;

public partial class Form1 : Form
{
    private static readonly string[] RoleNames =
        { "Filler", "Now", "Next", "NowSong", "NextSong", "Backin", "Overlay1", "Overlay2", "Overlay3", "Overlay4", "Promo" };
    private static readonly int[] AutomatedOverlayChannels = { 1, 2, 3, 4 };
    private static readonly int[] StaticOverlayChannels = { 1, 3, 4 }; // fixed-content overlays restored with a pinned input

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
    private bool _overlay2Visible;
    private DateTime _overlay2HideAt = DateTime.MinValue;

    // Repeats a Now-then-Next (or NowSong-then-NextSong) popup every configured interval —
    // NOW/NEXT Interval for a Program, NOWSong/NEXTSong Interval for the Filler — rather than
    // pegging it to any one item's own duration, since a Program (e.g. a multi-hour movie block)
    // or the Filler can run far longer than a single popup cycle.
    private readonly Queue<string> _overlay2Queue = new();
    private DateTime _overlay2NextCycleAt = DateTime.MinValue;

    // Promo fires independently of the schedule grid, on its own rolling cooldown from whenever
    // it last actually fired (not wall-clock aligned) — see HandlePromoAsync.
    private DateTime _lastPromoFiredAt = DateTime.MinValue;

    private const int AutoSyncIntervalSeconds = 15;
    private bool _isSyncing;
    private DateTime _lastAutoSync = DateTime.MinValue;
    private bool _hasSyncedOnce;
    private bool _isTickRunning;

    // True until (and again any time after) contact with vMix is lost — the app's own first sync
    // counts as "was unreachable" too, so HandleVmixReconnectedAsync's restore path is shared by
    // both cases instead of a once-per-app-lifetime flag that would miss a vMix-side restart the
    // app itself survives. See VmixReconnectPolicy.
    private bool _vmixUnreachable = true;

    // Caps the Log panel's TextBox so a multi-day run can't grow it (and its backing string)
    // without bound; trimmed back down to LogTrimTargetChars, not all the way, so it doesn't
    // re-trim on almost every subsequent line.
    private const int MaxLogChars = 200_000;
    private const int LogTrimTargetChars = 150_000;

    public Form1()
    {
        InitializeComponent();
        _client = new VmixClient();
        _lastPromoFiredAt = DateTime.Now;
        if (chkAutoStart.Checked)
            tmrCheck.Start();
        UpdateStartStopButtons();
        Log($"vMix Scheduler started (build {GetBuildHash()}).");
        Log("Rename vMix inputs per the naming convention — syncing automatically.");
        Log($"As-run log: {AppPaths.AsRunLogDirectory}");
        _ = SyncFromVmixAsync(silent: false);
    }

    private void btnStart_Click(object? sender, EventArgs e)
    {
        tmrCheck.Start();
        UpdateStartStopButtons();
        Log("Automation started.");
    }

    private void btnStop_Click(object? sender, EventArgs e)
    {
        tmrCheck.Stop();
        UpdateStartStopButtons();
        Log("Automation stopped.");
    }

    private void UpdateStartStopButtons()
    {
        btnStart.Enabled = !tmrCheck.Enabled;
        btnStop.Enabled = tmrCheck.Enabled;
    }

    // Short git commit hash embedded at build time via the SourceRevisionId MSBuild property
    // (see SetSourceRevisionId target in the .csproj), so anyone can tell which build is running
    // just from the Log panel instead of guessing from a stale installer.
    private static string GetBuildHash()
    {
        var infoVersion = System.Reflection.CustomAttributeExtensions
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(System.Reflection.Assembly.GetExecutingAssembly())
            ?.InformationalVersion;
        var hash = infoVersion?.Split('+') is { Length: 2 } parts ? parts[1] : null;
        return string.IsNullOrEmpty(hash) ? "unknown" : hash;
    }

    private int GetPort() => int.TryParse(txtPort.Text.Trim(), out var p) ? p : 8088;

    private int GetNowNextIntervalSeconds() => ParseIntervalSeconds(cmbNowNextInterval.SelectedItem?.ToString());

    /// <summary>Parses combo values like "20 min" or "10s" into seconds.</summary>
    private static int ParseIntervalSeconds(string? selection)
    {
        var trimmed = selection?.Trim() ?? "";
        if (trimmed.EndsWith("min", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(trimmed[..^3].Trim(), out var minutes) ? minutes * 60 : 5;
        if (trimmed.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(trimmed[..^1].Trim(), out var seconds) ? seconds : 5;
        return 5;
    }

    private int GetNowNextSongIntervalSeconds() => (int)numSongInterval.Value;

    private int GetNowNextDurationMs() => (int)numNowNextDuration.Value * 1000;

    private int GetSongDurationMs() => (int)numSongDuration.Value * 1000;

    private int GetTriggerOffsetSeconds() => (int)numTriggerOffset.Value;

    private int GetPromoIntervalSeconds() => (int)numPromoInterval.Value * 60;

    /// <summary>Ad/L-shape-ad rules only fire within this window; non-crossing-midnight (From &lt; To).</summary>
    private bool IsWithinAdsWindow(DateTime now)
    {
        var from = dtpAdsFrom.Value.TimeOfDay;
        var to = dtpAdsTo.Value.TimeOfDay;
        return now.TimeOfDay >= from && now.TimeOfDay < to;
    }

    private bool IsRuleAllowedNow(ScheduleRule rule, DateTime now) =>
        rule.Category is not (ScheduleCategory.Ad or ScheduleCategory.LShapeAd) || IsWithinAdsWindow(now);

    /// <summary>Shared by the auto-filler and Promo triggers so neither jumps in front of
    /// something that's about to fire on its own.</summary>
    private bool SomethingDueSoon(DateTime now) => _rules.Any(r =>
    {
        if (!IsRuleAllowedNow(r, now)) return false;
        var next = r.ComputeNextOccurrence(now);
        return next.HasValue && next.Value <= now.AddSeconds(3);
    });

    private void Log(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        TrimLogIfTooLong();
    }

    /// <summary>Keeps the Log panel bounded over a multi-day run — see MaxLogChars.</summary>
    private void TrimLogIfTooLong()
    {
        if (txtLog.TextLength <= MaxLogChars) return;
        var cutIndex = LogTrimmer.FindTrimIndex(txtLog.Text, LogTrimTargetChars);
        if (cutIndex <= 0) return;
        txtLog.Text = txtLog.Text[cutIndex..];
        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.ScrollToCaret();
    }

    private void LogThrottled(string message, DateTime now)
    {
        if ((now - _lastAutomationErrorLog).TotalSeconds < 30) return;
        _lastAutomationErrorLog = now;
        Log(message);
    }

    /// <summary>
    /// Appends one row to today's as-run CSV (AsRunLogs\as-run-yyyy-MM-dd.csv) — a durable record
    /// of exactly what aired and when, separate from the Log panel (which isn't persisted). Best
    /// effort: a write failure here must never interrupt live automation.
    /// </summary>
    private void RecordAsRun(string triggerType, string category, string displayName, string rawTitle, DateTime timestamp)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.AsRunLogDirectory);
            var path = Path.Combine(AppPaths.AsRunLogDirectory, $"as-run-{timestamp:yyyy-MM-dd}.csv");
            bool isNew = !File.Exists(path);
            using var writer = new StreamWriter(path, append: true);
            if (isNew) writer.WriteLine("Timestamp,TriggerType,Category,DisplayName,RawTitle");
            writer.WriteLine(string.Join(",",
                CsvUtil.EscapeField(timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                CsvUtil.EscapeField(triggerType),
                CsvUtil.EscapeField(category),
                CsvUtil.EscapeField(displayName),
                CsvUtil.EscapeField(rawTitle)));
        }
        catch (Exception ex)
        {
            LogThrottled($"As-run log write failed — {ex.Message}", timestamp);
        }
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
            await HandleVmixReconnectedAsync(host, port);
            lblConnectionStatus.Text = $"Connected — {inputs.Count} input(s), {_rules.Count} rule(s)";
            lblConnectionStatus.ForeColor = Color.SeaGreen;
            if (!silent)
                Log($"Synced from {host}:{port} — {inputs.Count} inputs, {_rules.Count} schedule rule(s) detected.");
        }
        catch (Exception ex)
        {
            _vmixUnreachable = true;
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

    /// <summary>
    /// Runs on every successful contact with vMix. Restores overlays whenever we're coming back
    /// from a period of unreachability — the app's own first-ever sync, or a reconnect after vMix
    /// itself restarted or hung mid-session (the app survives; only vMix dropped out). Without
    /// this, a vMix-side restart that happened to occur while overlays weren't suppressed for an ad
    /// would leave overlays 1/3/4 stuck off (vMix comes back with them off; the app's own
    /// _adOverlaysOff flag never transitioned, so nothing re-triggers the normal restore path) for
    /// however long it takes an ad break to fire and end afterward — potentially the rest of a
    /// multi-day broadcast. See VmixReconnectPolicy.
    /// </summary>
    private async Task HandleVmixReconnectedAsync(string host, int port)
    {
        if (!_vmixUnreachable) return;
        if (VmixReconnectPolicy.ShouldRestoreOverlaysOnReconnect(_vmixUnreachable, _adOverlaysOff))
            await RestoreAdOverlaysAsync(host, port, _hasSyncedOnce ? "Reconnected to vMix" : "Startup sync");
        _vmixUnreachable = false;
        _hasSyncedOnce = true;
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
        SetRoleLabel(lblRolePromoValue, "Promo");
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
        var now = DateTime.Now;
        try
        {
            await FireRuleAsync(host, port, rule, now);
            Log($"Manually triggered '{rule.DisplayName}'.");
            RecordAsRun("Manual", rule.Category.ToString(), rule.DisplayName, rule.RawTitle, now);
            RefreshGrid();
        }
        catch (Exception ex)
        {
            Log($"FAILED to manually trigger '{rule.DisplayName}' — {ex.Message}");
        }
    }

    private void btnViewAsRunLog_Click(object? sender, EventArgs e)
    {
        new AsRunLogViewerForm().Show(this);
    }

    // ---------- Firing rules ----------

    private async Task FireRuleAsync(string host, int port, ScheduleRule rule, DateTime occurrence)
    {
        await _client.TriggerInputAsync(host, port, rule.InputKey);
        rule.LastFiredOccurrence = occurrence;
        await PauseOutgoingInputAsync(host, port, _previousActiveKey, rule.InputKey);

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

    /// <summary>
    /// Freezes whatever's being cut away from so it can resume from this exact point later
    /// instead of drifting (or looping back to its own start) while off-air for however long the
    /// interruption lasts. Called after the cut completes, not before — pausing something that's
    /// still on Program would freeze the actual broadcast output for a moment.
    /// </summary>
    private async Task PauseOutgoingInputAsync(string host, int port, string? outgoingKey, string incomingKey)
    {
        if (string.IsNullOrEmpty(outgoingKey) || outgoingKey == incomingKey) return;
        try { await _client.PauseInputAsync(host, port, outgoingKey); }
        catch (Exception ex) { Log($"Pausing outgoing input failed — {ex.Message}"); }
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
                if (!IsRuleAllowedNow(r, now)) return false;
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
                    RecordAsRun("Scheduled", rule.Category.ToString(), rule.DisplayName, rule.RawTitle, now);
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
                // Marked here (not just in SyncFromVmixAsync's own catch) because this call runs
                // every tick (1s) while the sync only runs every AutoSyncIntervalSeconds — this is
                // usually the first thing to notice vMix drop out, and letting the sync path alone
                // detect the reconnect could leave overlays stuck for up to 15s longer than needed.
                _vmixUnreachable = true;
                LogThrottled($"Live automation: failed to reach vMix — {ex.Message}", now);
                return;
            }
            await HandleVmixReconnectedAsync(host, port);

            var active = status.FindActive();
            // Targets each title's primary field by position (index 0), not by name — the actual
            // field name varies per template (vMix's own default GT titles call it "Headline.Text";
            // imported/purchased .gtzip templates commonly name it something else entirely), so
            // going by position works for both instead of only whichever template happens to use
            // vMix's default field name.
            const int fieldIndex = 0;

            UpdateLiveStatusLabel(active);

            await UpdateNowNextAsync(host, port, active, fieldIndex, now);
            await UpdateNowNextSongAsync(host, port, active, fieldIndex, now);
            await UpdateBackinAsync(host, port, active, fieldIndex, now);
            await HandleAdOverlayStateAsync(host, port, active);
            await HandleAutoFillerAsync(host, port, active, now);
            await HandlePromoAsync(host, port, now);

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

    private async Task UpdateNowNextAsync(string host, int port, VmixInput? active, int fieldIndex, DateTime now)
    {
        var nowText = BestDisplayText(active);
        var nextText = active?.NextSongTitle ?? "";

        if (_roleInputs.TryGetValue("Now", out var nowInput))
            await TrySetText(host, port, nowInput.Key, fieldIndex, nowText, now);

        if (_roleInputs.TryGetValue("Next", out var nextInput))
            await TrySetText(host, port, nextInput.Key, fieldIndex, nextText, now);
    }

    /// <summary>
    /// Keeps NowSong/NextSong tracking the actively-playing Filler list item every tick. Writes
    /// unconditionally (blank when the Filler isn't active) rather than only writing while the
    /// Filler is playing — otherwise, the moment a Program takes over, these two fields simply
    /// stop being touched and vMix keeps showing whatever song was last playing, indefinitely.
    /// </summary>
    private async Task UpdateNowNextSongAsync(string host, int port, VmixInput? active, int fieldIndex, DateTime now)
    {
        bool isFillerActive = active != null && _roleInputs.TryGetValue("Filler", out var fillerForSong) && active.Number == fillerForSong.Number;
        var nowSongText = isFillerActive ? (active!.CurrentSongTitle ?? "") : "";
        var nextSongText = isFillerActive ? (active!.NextSongTitle ?? "") : "";

        if (_roleInputs.TryGetValue("NowSong", out var nowSongInput))
            await TrySetText(host, port, nowSongInput.Key, fieldIndex, nowSongText, now);

        if (_roleInputs.TryGetValue("NextSong", out var nextSongInput))
            await TrySetText(host, port, nextSongInput.Key, fieldIndex, nextSongText, now);
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
    /// Overlay2 is a shared "ticker" channel: every configured interval it pops up Now then Next
    /// (or NowSong then NextSong) back-to-back, then hides until the next interval elapses —
    /// NOW/NEXT Interval while a scheduled Program is on air, NOWSong/NEXTSong Interval while the
    /// Filler is on air.
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

        var intervalSeconds = isProgram ? GetNowNextIntervalSeconds() : GetNowNextSongIntervalSeconds();

        if (active.Key != _overlay2ItemKey)
        {
            // Actually tell vMix to turn it off (not just reset our own bookkeeping) — otherwise,
            // if a graphic was on-screen for the previous item at the exact moment it changed,
            // it stays visible until the next popup cycle happens to overwrite it.
            await HideOverlay2IfVisible(host, port);
            _overlay2ItemKey = active.Key;
            _overlay2Queue.Clear();
            // First popup for a newly-active item fires after the (short) Trigger Offset rather
            // than waiting a full interval — otherwise a fresh item gets no cue until an entire
            // NOW/NEXT (or Song) Interval has elapsed. Every later cycle for this same item still
            // follows the normal repeat cadence below.
            _overlay2NextCycleAt = now.AddSeconds(GetTriggerOffsetSeconds());
        }

        // Let whatever's currently showing finish its full visible window before considering
        // the next popup — otherwise, if the interval is shorter than one full display cycle,
        // it would replace itself every tick instead of holding for the popup duration.
        if (_overlay2Visible)
        {
            if (now < _overlay2HideAt) return;
            await HideOverlay2IfVisible(host, port);
        }

        if (_overlay2Queue.Count == 0)
        {
            if (now < _overlay2NextCycleAt) return;
            _overlay2Queue.Enqueue(isProgram ? "Now" : "NowSong");
            _overlay2Queue.Enqueue(isProgram ? "Next" : "NextSong");
            _overlay2NextCycleAt = now.AddSeconds(intervalSeconds);
        }

        await ShowOverlay2Async(host, port, _overlay2Queue.Dequeue(), now);
    }

    private async Task ShowOverlay2Async(string host, int port, string? role, DateTime now)
    {
        if (role == null || !_roleInputs.TryGetValue(role, out var showInput)) return;
        try
        {
            await _client.OverlayOnAsync(host, port, 2, showInput.Key);
            _overlay2Visible = true;
            bool isSongRole = role is "NowSong" or "NextSong";
            _overlay2HideAt = now.AddMilliseconds(isSongRole ? GetSongDurationMs() : GetNowNextDurationMs());
            Log($"Overlay2: showing '{role}'.");
        }
        catch (Exception ex)
        {
            LogThrottled($"Overlay2 show failed — {ex.Message}", now);
        }
    }

    private async Task HideOverlay2IfVisible(string host, int port)
    {
        if (!_overlay2Visible) return;
        try { await _client.OverlayOffAsync(host, port, 2); }
        catch { /* best effort */ }
        _overlay2Visible = false;
    }

    private async Task UpdateBackinAsync(string host, int port, VmixInput? active, int fieldIndex, DateTime now)
    {
        if (!_roleInputs.TryGetValue("Backin", out var backinInput)) return;

        var text = "--:--";
        if (active != null && active.Duration > 0)
        {
            var remainingMs = Math.Max(0, active.Duration - active.Position);
            text = TimeSpan.FromMilliseconds(remainingMs).ToString(@"mm\:ss");
        }
        await TrySetText(host, port, backinInput.Key, fieldIndex, text, now);
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
        if (active == null) return;

        // Position/Duration reflect only the currently-playing item within a list, not the list as
        // a whole — HasFinishedPlaying accounts for that, so a multi-item Program or Filler list
        // only counts as "finished" once it's actually on its last item, not just near the end of
        // whichever item happens to be playing.
        if (!active.HasFinishedPlaying()) return;
        if (now < _fillerCooldownUntil) return;
        if (SomethingDueSoon(now)) return;

        if (active.Number == filler.Number)
        {
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
                    await PauseOutgoingInputAsync(host, port, active.Key, resumeKey);
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
            await PauseOutgoingInputAsync(host, port, active.Key, filler.Key);
        }
        catch (Exception ex) { Log($"Auto-filler trigger failed — {ex.Message}"); }
    }

    /// <summary>
    /// Fires the Promo input every Promo Interval minutes, independent of the schedule grid —
    /// a brief interruption/restart like a scheduled Ad, but on its own rolling cooldown rather
    /// than a schedule-code occurrence. Skipped if something else is due within the next few
    /// seconds, or if Promo itself already fired within the current interval.
    /// </summary>
    private async Task HandlePromoAsync(string host, int port, DateTime now)
    {
        if (!_roleInputs.TryGetValue("Promo", out var promo)) return;
        if ((now - _lastPromoFiredAt).TotalSeconds < GetPromoIntervalSeconds()) return;
        if (SomethingDueSoon(now)) return;

        _lastPromoFiredAt = now;
        try
        {
            await _client.TriggerInputAsync(host, port, promo.Key);
            Log($"Promo: triggered '{promo.Name}'.");
            RecordAsRun("Promo", "Promo", promo.Name, promo.Name, now);
            await PauseOutgoingInputAsync(host, port, _previousActiveKey, promo.Key);
        }
        catch (Exception ex) { Log($"Promo trigger failed — {ex.Message}"); }
    }

    private void UpdateLiveStatusLabel(VmixInput? active)
    {
        if (active == null || active.Duration <= 0)
        {
            lblLiveStatus.Text = "Position: --:-- / Duration: --:-- / Remaining: --:--";
            return;
        }

        var position = TimeSpan.FromMilliseconds(active.Position).ToString(@"mm\:ss");
        var duration = TimeSpan.FromMilliseconds(active.Duration).ToString(@"mm\:ss");
        var remaining = TimeSpan.FromMilliseconds(Math.Max(0, active.Duration - active.Position)).ToString(@"mm\:ss");
        lblLiveStatus.Text = $"Position: {position} / Duration: {duration} / Remaining: {remaining}";
    }

    private async Task TrySetText(string host, int port, string inputKey, int fieldIndex, string value, DateTime now)
    {
        try
        {
            await _client.SetTextAsync(host, port, inputKey, fieldIndex, value);
        }
        catch (Exception ex)
        {
            LogThrottled($"Text overlay update failed (field index {fieldIndex}) — {ex.Message}", now);
        }
    }
}