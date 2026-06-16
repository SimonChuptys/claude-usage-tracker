# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## What this is

See [README.md](README.md)

## Behavioral Guidelines - Important

See [CLAUDE_GENERAL_GUIDELINES.md](CLAUDE_GENERAL_GUIDELINES.md)

Project-specific guidelines:
- Always keep documentsation up to date with implementation changes (README.md,
  comments, CLAUDE.md, OPEN_TASKS.md, ...)

## Open Tasks

See [OPEN_TASKS.md](OPEN_TASKS.md) for a list of tasks that still need implementing.

## Tech stack

- **.NET 8** (`net8.0-windows`), C#, nullable + implicit usings enabled.
- **Windows Forms** (`NotifyIcon`) for the tray UI — chosen because it is the
  simplest reliable way to host a notification-area icon. No XAML.
- Target IDE: **JetBrains Rider** (classic `.sln`; also opens in VS / `dotnet`).
- Windows-only by design (GDI icon rendering + `user32` interop).

## Build, run, test

```powershell
dotnet build                                  # build the solution
dotnet run --project src/ClaudeUsageTracker   # launch the tray app
dotnet publish src/ClaudeUsageTracker -c Release -r win-x64 --self-contained false
```

There is no test project yet; add one as `tests/ClaudeUsageTracker.Tests` and
register it in the solution when logic warrants coverage (the renderer and any
real provider are the natural first targets).

## Layout

```
ClaudeUsageTracker.sln
src/ClaudeUsageTracker/
  Program.cs                 # entry point; wires the IUsageProvider into the tray context
  TrayApplicationContext.cs  # tray icon, context menu, refresh timer — the app's UI
  TrayIconRenderer.cs        # draws the 16x16 ring icon for a remaining fraction
  NativeMethods.cs           # user32 DestroyIcon interop (frees GDI icon handles)
  Models/UsageSnapshot.cs    # UsageLimit + UsageSnapshot records
  Services/IUsageProvider.cs # abstraction over the usage data source
  Services/StubUsageProvider.cs # synthetic data so the UI runs without a backend
  Services/ClaudeCredentials.cs # resolves a valid token (signed-in / file / env / ~/.claude)
  Services/OAuthUsageProvider.cs # real provider: Claude Code /api/oauth/usage endpoint
  Services/OAuthTokens.cs    # signed-in tokens (access/refresh) persisted to %APPDATA%
  Services/ClaudeOAuthLogin.cs # in-app PKCE browser sign-in + token refresh
  Services/AuthRequiredException.cs # signals the tray to prompt sign-in
```

## Conventions

- One refresh path: `TrayApplicationContext.RefreshAsync` is the only place that
  pulls data and updates the icon/tooltip. Keep UI mutations on the UI thread.
- The tray icon is regenerated on each refresh; always dispose the previous
  `Icon` (and call `DestroyIcon` on raw HICONs) to avoid GDI handle leaks — see
  `TrayIconRenderer.Render`.
- `NotifyIcon.Text` is capped at 127 characters; keep tooltip building inside
  the existing `Truncate` guard.
