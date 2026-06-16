# Claude Usage Tracker

A Windows **system-tray (notification area) widget** that shows the remaining
Claude usage limits at a glance. The tray icon is a coloured ring whose fill and
colour reflect how much of the most-constrained limit is left; hovering shows a
tooltip with each tracked limit and its reset time.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Run

```powershell
dotnet run --project src/ClaudeUsageTracker
```

The app starts with no window — look for the ring icon in the notification area
(bottom-right of the taskbar). Right-click it for **Refresh now** and **Exit**.