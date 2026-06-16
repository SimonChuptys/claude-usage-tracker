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
    /// Fetches the latest usage snapshot. Should not throw for transient
    /// failures; return a best-effort snapshot or surface the error to the
    /// caller via an exception only for unrecoverable conditions.
    /// </summary>
    Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default);
}
