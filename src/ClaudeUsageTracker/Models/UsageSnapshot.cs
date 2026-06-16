namespace ClaudeUsageTracker.Models;

/// <summary>
/// A single Claude usage limit (e.g. the rolling 5-hour session limit or the
/// weekly limit) and how much of it remains.
/// </summary>
/// <param name="Name">Human-readable label, e.g. "Session (5h)" or "Weekly".</param>
/// <param name="UsedFraction">Fraction of the limit consumed, in the range 0.0–1.0.</param>
/// <param name="ResetsAt">When this limit next resets, if known.</param>
public sealed record UsageLimit(string Name, double UsedFraction, DateTimeOffset? ResetsAt)
{
    /// <summary>Fraction of the limit still available, 0.0–1.0.</summary>
    public double RemainingFraction => Math.Clamp(1.0 - UsedFraction, 0.0, 1.0);

    /// <summary>Percentage of the limit still available, 0–100.</summary>
    public int RemainingPercent => (int)Math.Round(RemainingFraction * 100);

    /// <summary>Percentage of the limit consumed, 0–100.</summary>
    public int UsedPercent => (int)Math.Round(Math.Clamp(UsedFraction, 0.0, 1.0) * 100);
}

/// <summary>
/// A point-in-time view of all tracked usage limits.
/// </summary>
/// <param name="Limits">The individual limits being tracked.</param>
/// <param name="RetrievedAt">When this snapshot was produced.</param>
public sealed record UsageSnapshot(IReadOnlyList<UsageLimit> Limits, DateTimeOffset RetrievedAt)
{
    /// <summary>
    /// The limit that is closest to being exhausted (lowest remaining), which
    /// is the most useful single number to surface in the tray icon.
    /// </summary>
    public UsageLimit? MostConstrained =>
        Limits.Count == 0 ? null : Limits.MinBy(l => l.RemainingFraction);
}
