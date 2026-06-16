using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using ClaudeUsageTracker.Models;

namespace ClaudeUsageTracker.Services;

/// <summary>
/// Real <see cref="IUsageProvider"/> backed by the OAuth-authenticated usage
/// endpoint Claude Code itself uses for <c>/usage</c>
/// (<c>GET https://api.anthropic.com/api/oauth/usage</c>). Surfaces the rolling
/// 5-hour session limit and the 7-day weekly limit.
/// </summary>
/// <remarks>
/// This is an unofficial endpoint and may change without notice. It also
/// rate-limits aggressively: poll no more often than ~180s, and always send the
/// <c>claude-code</c> User-Agent (omitting it triggers persistent 429s).
/// </remarks>
public sealed class OAuthUsageProvider : IUsageProvider
{
    private const string UsageUrl = "https://api.anthropic.com/api/oauth/usage";

    private static readonly HttpClient Http = new();
    private static readonly string UserAgent =
        "claude-code/" + (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0");

    public async Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        var token = await ClaudeCredentials.GetAccessTokenAsync(cancellationToken)
            ?? throw new AuthRequiredException("Sign in to Claude to track usage");

        using var request = new HttpRequestMessage(HttpMethod.Get, UsageUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("anthropic-beta", "oauth-2025-04-20");
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await Http.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new AuthRequiredException("Claude session expired — sign in again");
        }
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("Rate limited — try again shortly");
        }
        response.EnsureSuccessStatusCode();

        var usage = await response.Content.ReadFromJsonAsync<OAuthUsageResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Empty usage response");

        return new UsageSnapshot(MapLimits(usage), DateTimeOffset.Now);
    }

    // Maps the endpoint's utilization (0–100) windows to UsageLimits (UsedFraction 0–1).
    internal static IReadOnlyList<UsageLimit> MapLimits(OAuthUsageResponse usage)
    {
        var limits = new List<UsageLimit>(2);
        if (usage.FiveHour is { } session)
        {
            limits.Add(new UsageLimit(UsageLimitKind.Session, "Session (5h)", session.Utilization / 100.0, session.ResetsAt));
        }
        if (usage.SevenDay is { } weekly)
        {
            limits.Add(new UsageLimit(UsageLimitKind.Weekly, "Weekly", weekly.Utilization / 100.0, weekly.ResetsAt));
        }
        return limits;
    }

    internal sealed record OAuthUsageResponse(
        [property: JsonPropertyName("five_hour")] UsageWindow? FiveHour,
        [property: JsonPropertyName("seven_day")] UsageWindow? SevenDay);

    internal sealed record UsageWindow(
        [property: JsonPropertyName("utilization")] double Utilization,
        [property: JsonPropertyName("resets_at")] DateTimeOffset? ResetsAt);
}
