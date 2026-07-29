# vMix Scheduler — Installation Guide

A Windows desktop app that automates vMix scheduling, ads, overlays, and on-screen graphics by
reading naming conventions off your vMix inputs.

## Requirements

- Windows 10 or 11 (64-bit)
- vMix installed and running on the same PC (or reachable over the network)
- Nothing else — the published build is self-contained and does **not** require .NET to be
  installed separately.

## 1. Get the app

**Option A — Installer (recommended for other KMTV machines):**

Run `installer\output\VmixSchedulerSetup.exe`. It installs to Program Files, adds a Start Menu
shortcut (and optionally a desktop shortcut), and registers a normal Windows uninstaller.

> To rebuild the installer from source: publish the app first (see below), then from the
> `installer` folder run:
> ```
> "C:\Users\<you>\AppData\Local\Programs\Inno Setup 6\ISCC.exe" VmixScheduler.iss
> ```
> The compiled installer lands in `installer\output\VmixSchedulerSetup.exe`.

**Option B — Portable copy (for yourself / quick testing):**

The self-contained build lives at:

```
VmixScheduler\publish\VmixScheduler.exe
```

Copy that single file wherever you want it to live, e.g. `C:\KMTV\VmixScheduler\VmixScheduler.exe`
— no other files needed, no install wizard.

> To rebuild this file yourself from source, run from the `VmixScheduler` project folder:
> ```
> dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
> ```

Only one copy of the app can actively run at a time on a given machine — if you try to launch a
second one (e.g. a leftover portable copy while the installed version is already running), it'll
show a warning and refuse to open, rather than risk two schedulers fighting over the same vMix.

## 2. Enable vMix's Web Controller

The app talks to vMix over its local HTTP API, which is off by default.

1. Open vMix.
2. **Settings → Web Controller**.
3. Check **Enable Web Controller / API**.
4. Confirm the port is **8088** (the app's default — change both if you use a different port).

## 3. Run it

Double-click `VmixScheduler.exe`. On first launch it will:

- Try to connect to vMix at `127.0.0.1:8088` automatically.
- Sync every 15 seconds after that, so renamed inputs get picked up without clicking anything.
- Start automation immediately, unless you uncheck **Auto Start** (top of the window) — with it
  unchecked, use the **Start**/**Stop** buttons to control automation manually.

If vMix is on a different machine, change the **vMix Host** field to that machine's IP address
(and open port 8088 on its firewall).

## 4. Naming convention reference

Everything is driven by how you name/rename inputs **inside vMix** (right-click an input →
Rename, or double-click its title). The app never needs manual schedule entry — it reads these
names automatically.

### Schedule codes (rename an input to one of these)

| Rename to | Behavior |
|---|---|
| `HH:MM:SS/YYYY-MM-DD` | One-off program at that exact date/time |
| `Daily@HH:MM:SS` | Runs every day at that time |
| `Mon@HH:MM:SS` (Mon/Tue/Wed/Thu/Fri/Sat/Sun) | Weekly, that day |
| `Spon-Mon@HH:MM:SS` | Weekly sponsor ad |
| `Ad@HH:MM:SS` | Daily ad break at that time |
| `Ad@Every00:30:00` | Repeating ad, every interval, all day |
| `L@HH:MM:SS` / `L@Every00:25:00` | Same, for L-shape ads |

An optional label before the code (e.g. `MovieName Daily@19:00:00`) becomes the display name shown
in schedule grids and Now/Next graphics.

Ad and L-shape ad rules only fire within the **Ads From** / **Ads To** window (Automation Timing
panel, default 06:00–23:00) — outside that window they're simply skipped, even if otherwise due.

### Special role names (rename an input to exactly this, no code needed)

| Rename to | Purpose |
|---|---|
| `Filler` | Auto-plays whenever the active input finishes and nothing else is due |
| `Now` / `Next` | Title graphics showing the current/next program's file name |
| `NowSong` / `NextSong` | Same, but for whatever's currently playing inside Filler |
| `Backin` | Title graphic showing a countdown to the current item's end |
| `Promo` | Fires on its own repeating interval (Automation Timing panel), independent of the schedule grid — skipped if something else is due within a few seconds, or if it already fired within the current interval |
| `Overlay1`–`Overlay4` | Overlay channels — all four turn off automatically during any Ad/L-shape ad (whether triggered by this app or you cutting to it manually in vMix), then restore after. Overlay2 additionally cycles Now/Next/NowSong/NextSong graphics automatically during normal programming. |

### Automation Timing panel

| Control | What it does |
|---|---|
| NOW/NEXT Interval | How often the Now/Next popup repeats while a scheduled Program is on air |
| Now/Next Duration (s) | How long each Now/Next popup stays visible |
| Trigger Offset (s) | How soon after a new item goes active its *first* popup appears (later popups follow the Interval above) |
| Song Interval (s) | How often the NowSong/NextSong popup repeats while the Filler is on air |
| Song Duration (s) | How long each NowSong/NextSong popup stays visible |
| Promo Interval (min) | How often the `Promo` input fires |
| Ads From / Ads To | Time-of-day window Ad/L-shape ad rules are allowed to fire in |

## 5. Where the app keeps its records

The app writes to `%ProgramData%\VmixScheduler\` (not the install folder — a normal, non-elevated
launch can't write to Program Files):

- `crash.log` — any unhandled error, with a timestamp, so a crash mid-show can be diagnosed after
  the fact instead of just vanishing.
- `AsRunLogs\as-run-yyyy-MM-dd.csv` — one row per fire (scheduled, manual, or Promo) with
  timestamp, category, and title — a durable proof-of-air record for sponsors/ad buyers, separate
  from the Log panel (which isn't persisted).

The Log panel also shows the running build's short git commit hash on startup (e.g. `build
8fb8cb4`), so you can always tell which version is actually running.

## 6. Quick test checklist

- [ ] Web Controller enabled in vMix, app shows "Connected — N input(s), N rule(s)"
- [ ] Roles panel shows your renamed inputs in green (not "(not found)")
- [ ] Grid shows your schedule-coded inputs with a computed "Next Occurrence"
- [ ] Select a schedule row → **Trigger Selected Now** to force-test without waiting
- [ ] Watch the log panel at the bottom for what it's doing each second
- [ ] Switch between a Program and the Filler and confirm Now/Next/NowSong/NextSong update
      immediately rather than showing the previous item's title

## Troubleshooting

- **Roles show "(not found)"** — double-check the exact spelling/casing in vMix; matching is
  case-insensitive but must otherwise match exactly (no extra spaces).
- **Text graphics don't update** — the app targets a title's *first* text field by position, not
  by name, so it works the same whether the title is a plain `.xaml` template or an imported
  `.gtzip` one. If it's still not updating, confirm the input's own title/graphic actually has a
  text field at all (some GT templates use the first field for something other than the main
  headline) — check in vMix's Title Designer.
- **Nothing fires on schedule** — check your PC's system clock/timezone; schedule times are
  matched against local time. Also confirm the time is within the Ads From/To window if it's an
  Ad or L-shape ad rule.
- **Connection failed** — confirm vMix is running, Web Controller is enabled, and the host/port in
  the app match vMix's settings.
- **"Another instance is already running"** — a second copy detected the first one already
  controlling vMix and refused to open, on purpose. Close the other copy first.
