# Open Tasks

A list of taks that are not yet implemented, with guidance and progress reports.

## 1. Usage data source (the main TODO)

`StubUsageProvider` returns **fake** data so the UI is runnable today. The real
work is implementing `IUsageProvider` against an actual source of Claude usage
limits, then swapping it in at the single wire-up point in `Program.cs`.

When implementing a real provider:
- Keep the `IUsageProvider` contract: return a best-effort `UsageSnapshot`;
  surface only unrecoverable conditions as exceptions (the tray catches and
  shows transient errors in the tooltip).
- Map each distinct limit (e.g. rolling session limit, weekly limit) to one
  `UsageLimit` with a `UsedFraction` in 0.0–1.0 and a `ResetsAt` when known.
- **Never commit credentials.** Read tokens/paths from user config or
  environment; local config files are git-ignored (`*.secrets.json`, `.env`,
  `appsettings.*.local.json`).

## 2. UI design

The current circular icon is placeholder. The final UI should be a usage percent along with progress bar filled according to the usage percentage.
The bar should be color coded (green-yellow-red) according to usage remaining.
