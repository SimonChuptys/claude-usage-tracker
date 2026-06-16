# Open Tasks

A list of taks that are not yet implemented, with guidance and progress reports.

## 1. Usage data source — DONE (v1)

`OAuthUsageProvider` fetches **real** session (5h) and weekly limits from the
OAuth-authenticated endpoint Claude Code uses for `/usage`
(`GET https://api.anthropic.com/api/oauth/usage`). Wired in `Program.cs`;
`StubUsageProvider` stays for offline UI dev.

**Authentication is in-app.** `ClaudeOAuthLogin` runs the Claude OAuth PKCE flow
(browser → localhost loopback capture → token exchange), triggered user-initiated
from the tray (clickable error balloon or the "Sign in to Claude…" menu item).
Tokens persist to `%APPDATA%\ClaudeUsageTracker\tokens.json` and
`ClaudeCredentials` refreshes the access token automatically via the refresh
token. Fallback token sources (checked when not signed in): the
`%APPDATA%\ClaudeUsageTracker\token` file (`claude setup-token` output), the
`CLAUDE_CODE_OAUTH_TOKEN` env var, then the Claude Code CLI login at
`~/.claude/.credentials.json`. When no token is usable the provider throws
`AuthRequiredException` and the tray prompts sign-in.

Caveats / notes:
- The usage endpoint **and** the OAuth login client/endpoints are **unofficial**
  and may change without notice. The usage endpoint also rate-limits aggressively
  — refresh is 5 min and requests send the `claude-code` User-Agent.
- The in-app OAuth flow uses a localhost-loopback redirect and the documented
  Claude Code scopes; needs live testing on a real machine (browser sign-in can't
  be exercised in CI/headless).

Remaining follow-ups:
- **Keep last good snapshot** on transient 429/5xx instead of showing the error
  icon/tooltip each time.
- Fallback to the **headless/manual code** OAuth flow if a host blocks the
  loopback redirect.

When extending the provider, keep the `IUsageProvider` contract: return a
best-effort `UsageSnapshot`; surface only unrecoverable conditions as exceptions
(the tray catches them, shows a red error icon, and puts the message in the
tooltip). Map each limit to one `UsageLimit` with a `UsedFraction` in 0.0–1.0 and
a `ResetsAt` when known. **Never commit credentials** — local config files are
git-ignored (`*.secrets.json`, `.env`, `appsettings.*.local.json`).

## 2. UI design — DONE

The icon shows the **full percentage** (e.g. "83", "100") above a **full-width
horizontal progress bar** that fills left-to-right as usage grows:

- Rendered at a larger native size (32px) so it stays crisp when Windows scales
  it for high-DPI taskbars (a fixed 16px bitmap would upscale blurry).
- The bar's fill is proportional to the used fraction and colour-coded
  green → amber → red as usage grows (`TrayIconRenderer.ColorFor`); the
  percentage is drawn in white above the bar.

The icon reflects the most-constrained limit; the hover tooltip continues to list
all tracked limits. See `TrayIconRenderer.cs`.
