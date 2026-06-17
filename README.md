# Claude Usage Tracker

A Windows **system-tray (notification area) widget** that shows how much of your
Claude subscription usage limits is consumed at a glance. The tray icon shows the session (5h) usage
percentage above two stacked progress bars — the session (5h) limit on top and
the weekly limit below — that fill as usage grows; each bar's fill and colour
reflect how much of that limit is used. Hovering shows a tooltip with each tracked limit and its
reset time.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A Claude Pro/Max subscription. The tracker reads your live session/weekly usage
  through your Claude sign-in — no developer API key needed.

## Authentication

The tracker works if you're already signed in to **[Claude Code](https://claude.com/claude-code)**
— it reuses that login
automatically.

Otherwise, **sign in from the tray**: when no sign-in is found the icon turns red
and a notification appears — click it (or right-click the icon → **Sign in to
Claude…**). Your browser opens to the Claude login; approve it and you're
returned to the app. You normally need to sign in only once.

## Run

```powershell
dotnet run --project src/ClaudeUsageTracker
```

The app starts with no window — look for the ring icon in the notification area
(bottom-right of the taskbar). Right-click it for **Refresh now** and **Exit**.

## Download

Prebuilt, self-contained Windows executables are published on the repo's
[Releases page](https://github.com/SimonChuptys/claude-usage-tracker/releases).
Download the latest `ClaudeUsageTracker-v*-win-x64.exe` and run it — no .NET
install required. (Windows SmartScreen may warn on first run since the build is
unsigned; choose **More info → Run anyway**.)

## Releasing

Releases are automated by the [`Release` workflow](.github/workflows/release.yml),
which runs on every push to the **`release`** branch:

1. Bump `<Version>` in
   [`src/ClaudeUsageTracker/ClaudeUsageTracker.csproj`](src/ClaudeUsageTracker/ClaudeUsageTracker.csproj)
   (e.g. `0.1.0` → `0.2.0`).
2. Commit and push the change to the `release` branch.
3. The workflow runs the tests, and if the version's tag (`v{version}`) does not
   already exist, builds the single-file exe, creates a GitHub Release with the
   exe attached, and tags the commit.

Pushing to `release` without bumping the version is a no-op — the existing tag
makes the run skip the build/publish steps, so no duplicate release is created.