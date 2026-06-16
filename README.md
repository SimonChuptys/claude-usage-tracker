# Claude Usage Tracker

A Windows **system-tray (notification area) widget** that shows how much of your
Claude subscription usage limits is consumed at a glance. The tray icon shows the usage percentage above a
horizontal progress bar that fills as usage grows; its fill and colour reflect
how much of the most-constrained limit is used (green when little is used, red
when nearly exhausted). Hovering shows a tooltip with each tracked limit and its
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