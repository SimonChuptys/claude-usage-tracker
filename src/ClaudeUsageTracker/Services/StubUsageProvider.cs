using ClaudeUsageTracker.Models;

namespace ClaudeUsageTracker.Services;

/// <summary>
/// Placeholder provider that returns synthetic data so the UI can be developed
/// and run before a real data source is connected. Replace this with a real
/// <see cref="IUsageProvider"/> (see OPEN_TASKS.md "Usage data source").
/// </summary>
public sealed class StubUsageProvider : IUsageProvider
{
    private readonly DateTimeOffset _started = DateTimeOffset.Now;

    public Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        // Slowly ramp usage over time so the icon visibly changes while testing.
        var minutesElapsed = (DateTimeOffset.Now - _started).TotalMinutes;
        var sessionUsed = Math.Clamp(minutesElapsed / 60.0, 0.0, 1.0);

        var limits = new List<UsageLimit>
        {
            new(UsageLimitKind.Session, "Session (5h)", sessionUsed, DateTimeOffset.Now.AddHours(5)),
            new(UsageLimitKind.Weekly, "Weekly", 0.42, DateTimeOffset.Now.AddDays(3)),
        };

        return Task.FromResult(new UsageSnapshot(limits, DateTimeOffset.Now));
    }
}
