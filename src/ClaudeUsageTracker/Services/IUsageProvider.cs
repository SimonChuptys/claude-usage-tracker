using ClaudeUsageTracker.Models;

namespace ClaudeUsageTracker.Services;

/// <summary>
/// Supplies the current Claude usage limits. Implementations are responsible
/// for talking to whatever the underlying data source is (local Claude Code
/// state, an API, etc.) and translating it into a <see cref="UsageSnapshot"/>.
/// </summary>
public interface IUsageProvider
{
    /// <summary>
    /// Fetches the latest usage snapshot. Transient failures (e.g. HTTP 429 or
    /// 5xx) are currently surfaced as exceptions, which the tray shows as the red
    /// error icon; serving the last good snapshot on transient errors is a
    /// tracked follow-up (see OPEN_TASKS.md #1). Conditions needing user action
    /// (no usable token) are surfaced as <see cref="AuthRequiredException"/>.
    /// </summary>
    Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default);
}
