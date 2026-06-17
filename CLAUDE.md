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
dotnet test                                   # run the unit tests
dotnet publish src/ClaudeUsageTracker -c Release -r win-x64 --self-contained false
```

Unit tests live in `tests/ClaudeUsageTracker.Tests` (xUnit) and cover the pure
logic: `UsageSnapshot`/`UsageLimit`, `TrayIconRenderer.ColorFor`, the OAuth
parsing helpers, and `OAuthUsageProvider.MapLimits`. Internals are exposed to the
test assembly via `InternalsVisibleTo` in the app's csproj. Note: `dotnet test`
rebuilds the app exe, which fails if a tray instance is running and holding the
file lock — close it first.

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
tests/ClaudeUsageTracker.Tests/ # xUnit tests for the pure logic
.github/workflows/release.yml # CI release pipeline (push to `release` branch)
```

## Releases

`.github/workflows/release.yml` runs on every push to the `release` branch: it
runs the tests, reads `<Version>` from the app csproj, and — if no `v{version}`
git tag exists yet — publishes a self-contained single-file `win-x64` exe,
creates a GitHub Release with the exe attached, and tags the commit. The
csproj `<Version>` is the single source of truth; bump it to cut a release. See
the **Releasing** section in [README.md](README.md).

## Conventions

- One refresh path: `TrayApplicationContext.RefreshAsync` is the only place that
  pulls data and updates the icon/tooltip. Keep UI mutations on the UI thread.
- The tray icon is regenerated on each refresh; always dispose the previous
  `Icon` (and call `DestroyIcon` on raw HICONs) to avoid GDI handle leaks — see
  `TrayIconRenderer.Render`.
- `NotifyIcon.Text` is capped at 127 characters; keep tooltip building inside
  the existing `Truncate` guard.
