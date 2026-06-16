# Claude Usage Tracker

A Windows **system-tray (notification area) widget** that shows how much of your
Claude usage limits is consumed at a glance. The tray icon is a coloured ring
whose fill and colour reflect how much of the most-constrained limit is used
(green when little is used, red when nearly exhausted); hovering shows a tooltip
with each tracked limit and its reset time.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A Claude Pro/Max subscription and an OAuth token on this machine. The tracker
  reads your live session/weekly usage — no developer API key needed. If no token
  is found, the tray shows a red error icon with the reason on hover.

## Authentication

**Sign in from the tray (recommended).** When no token is found, the icon turns
red and a notification appears — click it (or right-click the icon → **Sign in to
Claude…**). Your browser opens to the Claude login; approve it and the window
returns you to the app. The tokens are saved to
`%APPDATA%\ClaudeUsageTracker\tokens.json` and refreshed automatically, so you
normally sign in only once.

Alternative token sources (checked if you haven't signed in):

- A long-lived token from `claude setup-token` saved (just the token string) to
  `%APPDATA%\ClaudeUsageTracker\token`.
- A `CLAUDE_CODE_OAUTH_TOKEN` environment variable.
- A logged-in [Claude Code CLI](https://claude.com/claude-code)
  (`~/.claude/.credentials.json`).

Keep these tokens private — they grant access to your Claude account.

## Run

```powershell
dotnet run --project src/ClaudeUsageTracker
```

The app starts with no window — look for the ring icon in the notification area
(bottom-right of the taskbar). Right-click it for **Refresh now** and **Exit**.