using ClaudeUsageTracker.Models;
using Xunit;

namespace ClaudeUsageTracker.Tests;

public class UsageSnapshotTests
{
    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.234, 23)]
    [InlineData(0.235, 24)]   // rounds to nearest
    [InlineData(1.5, 100)]    // clamped above 1.0
    [InlineData(-0.5, 0)]     // clamped below 0.0
    public void UsedPercent_clamps_and_rounds(double fraction, int expected)
    {
        Assert.Equal(expected, new UsageLimit("x", fraction, null).UsedPercent);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.25, 0.75)]
    [InlineData(1.5, 0.0)]     // clamped
    public void RemainingFraction_is_one_minus_used_clamped(double used, double expected)
    {
        Assert.Equal(expected, new UsageLimit("x", used, null).RemainingFraction, 3);
    }

    [Fact]
    public void MostConstrained_returns_null_when_empty()
    {
        var snapshot = new UsageSnapshot(new List<UsageLimit>(), DateTimeOffset.Now);
        Assert.Null(snapshot.MostConstrained);
    }

    [Fact]
    public void MostConstrained_picks_lowest_remaining()
    {
        var session = new UsageLimit("Session (5h)", 0.30, null);
        var weekly = new UsageLimit("Weekly", 0.80, null);
        var snapshot = new UsageSnapshot(new[] { session, weekly }, DateTimeOffset.Now);

        Assert.Same(weekly, snapshot.MostConstrained);
    }
}
