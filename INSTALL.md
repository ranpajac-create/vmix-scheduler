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

### Special role names (rename an input to exactly this, no code needed)

| Rename to | Purpose |
|---|---|
| `Filler` | Auto-plays whenever the active input finishes and nothing else is due |
| `Now` / `Next` | Title graphics showing the current/next program's file name |
| `NowSong` / `NextSong` | Same, but for whatever's currently playing inside Filler |
| `Backin` | Title graphic showing a countdown to the current item's end |
| `Overlay1`–`Overlay4` | Overlay channels — all four turn off automatically during any Ad/L-shape ad (whether triggered by this app or you cutting to it manually in vMix), then restore after. Overlay2 additionally cycles Now/Next/NowSong/NextSong graphics automatically during normal programming. |

## 5. Quick test checklist

- [ ] Web Controller enabled in vMix, app shows "Connected — N input(s), N rule(s)"
- [ ] Roles panel shows your renamed inputs in green (not "(not found)")
- [ ] Grid shows your schedule-coded inputs with a computed "Next Occurrence"
- [ ] Select a schedule row → **Trigger Selected Now** to force-test without waiting
- [ ] Watch the log panel at the bottom for what it's doing each second

## Troubleshooting

- **Roles show "(not found)"** — double-check the exact spelling/casing in vMix; matching is
  case-insensitive but must otherwise match exactly (no extra spaces).
- **Text graphics don't update** — the app pushes text into a field named `Headline.Text` by
  default (the **Title Field Name** box in the app). If your title template uses a different field
  name for its text layer, change that box to match.
- **Nothing fires on schedule** — check your PC's system clock/timezone; schedule times are
  matched against local time.
- **Connection failed** — confirm vMix is running, Web Controller is enabled, and the host/port in
  the app match vMix's settings.
