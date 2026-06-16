using System.Text.Json;
using ClaudeUsageTracker.Models;
using ClaudeUsageTracker.Services;
using Xunit;

namespace ClaudeUsageTracker.Tests;

public class OAuthUsageProviderTests
{
    private static OAuthUsageProvider.OAuthUsageResponse Parse(string json) =>
        JsonSerializer.Deserialize<OAuthUsageProvider.OAuthUsageResponse>(json)!;

    [Fact]
    public void MapLimits_maps_both_windows_with_utilization_as_fraction()
    {
        var usage = Parse("""
            {
              "five_hour": { "utilization": 25, "resets_at": "2026-06-16T18:00:00+00:00" },
              "seven_day": { "utilization": 80, "resets_at": "2026-06-20T00:00:00+00:00" }
            }
            """);

        var limits = OAuthUsageProvider.MapLimits(usage);

        Assert.Collection(limits,
            l =>
            {
                Assert.Equal(UsageLimitKind.Session, l.Kind);
                Assert.Equal("Session (5h)", l.Name);
                Assert.Equal(0.25, l.UsedFraction, 3);
                Assert.Equal(new DateTimeOffset(2026, 6, 16, 18, 0, 0, TimeSpan.Zero), l.ResetsAt);
            },
            l =>
            {
                Assert.Equal(UsageLimitKind.Weekly, l.Kind);
                Assert.Equal("Weekly", l.Name);
                Assert.Equal(0.80, l.UsedFraction, 3);
            });
    }

    [Fact]
    public void MapLimits_omits_missing_windows()
    {
        var usage = Parse("""{ "five_hour": { "utilization": 10, "resets_at": null } }""");

        var limit = Assert.Single(OAuthUsageProvider.MapLimits(usage));
        Assert.Equal("Session (5h)", limit.Name);
        Assert.Null(limit.ResetsAt);
    }

    [Fact]
    public void MapLimits_returns_empty_when_no_windows()
    {
        Assert.Empty(OAuthUsageProvider.MapLimits(Parse("{}")));
    }
}
