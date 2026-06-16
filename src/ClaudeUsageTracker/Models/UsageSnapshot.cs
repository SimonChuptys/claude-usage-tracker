namespace ClaudeUsageTracker.Models;

/// <summary>
/// Which Claude usage limit a <see cref="UsageLimit"/> represents.
/// </summary>
public enum UsageLimitKind
{
    /// <summary>The rolling 5-hour session limit.</summary>
    Session,

    /// <summary>The 7-day weekly limit.</summary>
    Weekly,
}

/// <summary>
/// A single Claude usage limit (e.g. the rolling 5-hour session limit or the
/// weekly limit) and how much of it remains.
/// </summary>
/// <param name="Kind">Which limit this is (session or weekly).</param>
/// <param name="Name">Human-readable label, e.g. "Session (5h)" or "Weekly".</param>
/// <param name="UsedFraction">Fraction of the limit consumed, in the range 0.0–1.0.</param>
/// <param name="ResetsAt">When this limit next resets, if known.</param>
public sealed record UsageLimit(UsageLimitKind Kind, string Name, double UsedFraction, DateTimeOffset? ResetsAt)
{
    /// <summary>Fraction of the limit still available, 0.0–1.0.</summary>
    public double RemainingFraction => Math.Clamp(1.0 - UsedFraction, 0.0, 1.0);

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

    /// <summary>The tracked limit of the given kind, or null if not present.</summary>
    public UsageLimit? Limit(UsageLimitKind kind) =>
        Limits.FirstOrDefault(l => l.Kind == kind);
}
