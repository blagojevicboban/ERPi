---
name: run-erpi-app
description: Build, launch, and drive the ERPiApp WPF desktop app (screenshot, click, type) via a UI Automation PowerShell driver. Use when asked to run, start, build, test, or screenshot ERPiApp / ERPi, or to verify a WPF/XAML UI change actually works.
---

Paths below are relative to `ERPi/` (the repo root — it contains `ERPi.slnx`). ERPiApp is
a .NET 8 WPF desktop app (`net8.0-windows`, code-behind, no MVVM), run natively on
Windows — there is no headless/xvfb story here, the app just runs. Drive it via
`driver.ps1` in this directory.

## Database: AUTOTEST is the sanctioned test company, no isolation needed

`--autologin` (see below) always opens/creates one fixed company database at
`%LocalAppData%\ERPi\Baze\AUTOTEST.db` — a throwaway test company, not real data. Driving
the app against it is intentional. To reset it to empty: just delete the file (it's
recreated with a fresh Core schema + seed admin on next `--autologin` launch).

```powershell
Remove-Item "$env:LOCALAPPDATA\ERPi\Baze\AUTOTEST.db" -ErrorAction SilentlyContinue
```

## Run (agent path) — build, launch, drive, screenshot

### 1. Build

```powershell
dotnet build ERPi.slnx -c Debug
```

Produces `ERPiApp\bin\Debug\net8.0-windows\ERPiApp.exe`.

### 2. Drive it

All commands go through `driver.ps1` (this directory). Each invocation is a fresh
`powershell.exe` process; it tracks the running app's PID in
`$env:TEMP\erpiapp_driver_state.json` so successive calls find the same window.

```powershell
# `launch` logs in on its own and returns only once MainWindow is up — no credentials
# to type, and no need to sleep before the first `click`.
powershell -ExecutionPolicy Bypass -File "C:\ERPi\ERPi\ERPiApp\.claude\skills\run-erpi-app\driver.ps1" launch "C:\ERPi\ERPi\ERPiApp\bin\Debug\net8.0-windows\ERPiApp.exe"
powershell -ExecutionPolicy Bypass -File "C:\ERPi\ERPi\ERPiApp\.claude\skills\run-erpi-app\driver.ps1" tree                  # dump AutomationId tree of current window
powershell -ExecutionPolicy Bypass -File "C:\ERPi\ERPi\ERPiApp\.claude\skills\run-erpi-app\driver.ps1" click BtnNalozi        # any AutomationId from `tree`, e.g. BtnDashboard/BtnNalozi
powershell -ExecutionPolicy Bypass -File "C:\ERPi\ERPi\ERPiApp\.claude\skills\run-erpi-app\driver.ps1" ss nalozi.png
powershell -ExecutionPolicy Bypass -File "C:\ERPi\ERPi\ERPiApp\.claude\skills\run-erpi-app\driver.ps1" close
```

`Stop-Process -Name ERPiApp*` clears a file lock before rebuilding if the app is still running.

Commands: `launch <exe> [nologin]`, `tree`, `click <AutomationId>`, `type <AutomationId> <text>`,
`keys <SendKeys-string>`, `clicktype <AutomationId> <SendKeys-string>`, `ss <out.png>`,
`close`. `AutomationId` is the control's `x:Name` in XAML (WPF exposes it 1:1 to UI
Automation for named elements).

### Login: `launch` handles it, do not type credentials

`launch` passes `--autologin` to the app, which opens/creates the AUTOTEST company and
`MainWindow` straight away as the first active administrator, and returns only once that
window exists. Nothing is typed — SendKeys toward `CompanySelectWindow`/`LoginWindow`
(username/password fields, a `PasswordBox`) is unreliable in this environment.

`--autologin` is wrapped in `#if DEBUG` in [App.xaml.cs](../../../App.xaml.cs) — the code
is not compiled into Release builds, so the shipped application cannot be entered this way.
It exists solely for this driver.

Pass `nologin` as the second argument (`launch <exe> nologin`) when `CompanySelectWindow`
or `LoginWindow` themselves are what you need to see or screenshot.

## Run (human path)

Visual Studio 2022+ / Rider: open `ERPi.slnx`, set `ERPiApp` as startup project, F5. Or
`dotnet run --project ERPiApp\ERPiApp.csproj` — opens a real window, blocks until closed;
useless for an agent without the driver above.

## Gotchas

- **`RadioButton` (the sidebar nav — `BtnDashboard`/`BtnNalozi`/...) does NOT support
  `InvokePattern`.** Only plain `Button` does. Calling `InvokePattern.Invoke()` on a
  `RadioButton` throws "Unsupported Pattern." Using `SelectionItemPattern.Select()` or
  `TogglePattern.Toggle()` instead "succeeds" (the button visibly becomes selected/blue)
  but **does not raise the WPF `Click` event** — so `MainWindow`'s `Click="NavXxx_Click"`
  handlers silently never fire and the content pane never actually switches. Fix: focus
  the element then send a **Space key press** — WPF's `ButtonBase` (base of both `Button`
  and `RadioButton`) treats Space as a click-equivalent and reliably raises `Click`.
  `driver.ps1`'s `click` command already does Invoke-first-else-Space.
- **`PasswordBox` has no `ValuePattern`.** UI Automation can't set its value directly (by
  design). `driver.ps1`'s `type` command uses `SendKeys` uniformly for both `TextBox` and
  `PasswordBox`.
- **Screenshots use UIA `BoundingRectangle`, not a raw `user32!GetWindowRect` call from
  this script.** `GetWindowRect` is reported in whatever DPI-awareness context the
  *calling* process declares — a plain, non-DPI-aware `powershell.exe` under-reports a
  window's true pixel size on any scaled display (125%/150%+), so every screenshot
  silently crops the window's right/bottom edge with no error. Cost real time to track
  down once (see `ERPi/ANALIZA_I_PLAN.md` session history if curious) — `BoundingRectangle`
  does not have this problem, UI Automation resolves it correctly regardless of the
  caller's own DPI awareness. Don't "simplify" `ss` back to a raw `GetWindowRect` call.
- **`SetForegroundWindow` gets silently denied on repeat calls.** Windows' foreground-lock
  heuristic blocks a background process from repeatedly stealing focus — symptom is a
  screenshot that shows your editor/terminal instead of the app, no error raised. Fix: go
  through `(New-Object -ComObject WScript.Shell).AppActivate($pid)` instead of raw
  `user32!SetForegroundWindow`. `driver.ps1`'s `Get-TopWindow` already does this before
  every `ss`/`click`/`type`.
- **A native `MessageBox` is a separate top-level window for the same process.**
  `Get-TopWindow` picks the *last* window for the tracked PID, so `tree`/`click`/`ss`
  transparently target the dialog once one is open — use `tree` to find its OK button's
  `AutomationId` (it won't have a friendly `x:Name`, e.g. `AutomationId='2'`).
- **An open `ComboBox` dropdown (`IsDropDownOpen=true`, e.g. the Konto/Partner columns in
  `NalogEditWindow`'s stavke grid) is a separate top-level Popup hwnd, and it auto-closes
  the instant the window is reactivated** — which `Get-TopWindow` does on *every*
  `driver.ps1` call (`AppActivate`). So there is no way with this driver to open a
  dropdown in one call and then inspect/click one of its rows in a later call — use
  `clicktype` (opens + types + `{ENTER}` in the *same* activation cycle) instead of
  separate `click` + `keys` calls for anything involving a dropdown.

## Troubleshooting

- **`FindFirst` returns `$null` / "Element with AutomationId 'X' not found"**: run
  `driver.ps1 tree` first to confirm the current window and its actual `AutomationId`s —
  you may be on a `MessageBox` dialog (see Gotchas) or `CompanySelectWindow`/`LoginWindow`
  rather than `MainWindow`.
- **Screenshot captures the wrong window (editor/terminal instead of the app)**: see the
  `SetForegroundWindow` gotcha above.
- **Clicking a sidebar nav item does nothing (dashboard stays visible)**: see the
  `RadioButton` gotcha above.
