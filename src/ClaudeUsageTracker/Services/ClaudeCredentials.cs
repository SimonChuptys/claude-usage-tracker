using System.Text.Json;

namespace ClaudeUsageTracker.Services;

/// <summary>
/// Supplies a usable Claude OAuth access token. Looks, in order, at:
/// <list type="number">
/// <item>tokens from the in-app sign-in (<see cref="OAuthTokens"/>), refreshing
/// automatically when the access token has expired;</item>
/// <item>the user-managed token file <see cref="TokenFilePath"/> (paste the
/// output of <c>claude setup-token</c> here);</item>
/// <item>the <c>CLAUDE_CODE_OAUTH_TOKEN</c> environment variable;</item>
/// <item>the Claude Code CLI login at
/// <c>%USERPROFILE%\.claude\.credentials.json</c> (<c>claudeAiOauth.accessToken</c>).</item>
/// </list>
/// </summary>
internal static class ClaudeCredentials
{
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromSeconds(60);

    /// <summary>
    /// User-managed token file: <c>%APPDATA%\ClaudeUsageTracker\token</c>. Should
    /// contain just the token string (output of <c>claude setup-token</c>).
    /// </summary>
    public static string TokenFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClaudeUsageTracker",
        "token");

    /// <summary>
    /// Returns a currently-valid access token, or <c>null</c> if none can be
    /// obtained (the caller should then prompt the user to sign in). Read fresh
    /// on every call; refreshes the stored session in place when needed.
    /// </summary>
    public static async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var tokens = OAuthTokens.Load();
        if (tokens is not null)
        {
            if (!tokens.IsExpired(ExpiryMargin))
            {
                return tokens.AccessToken;
            }
            if (!string.IsNullOrEmpty(tokens.RefreshToken))
            {
                try
                {
                    var refreshed = await ClaudeOAuthLogin.RefreshAsync(tokens.RefreshToken, cancellationToken);
                    // Anthropic may rotate the refresh token; keep the old one if absent.
                    refreshed = refreshed with { RefreshToken = refreshed.RefreshToken ?? tokens.RefreshToken };
                    refreshed.Save();
                    return refreshed.AccessToken;
                }
                catch
                {
                    // Refresh failed (revoked/expired) — fall through to other
                    // sources, then ultimately a sign-in prompt.
                }
            }
        }

        return TryGetStaticToken();
    }

    // File / env / CLI sources — already-valid token strings, no refresh.
    private static string? TryGetStaticToken()
    {
        if (File.Exists(TokenFilePath))
        {
            try
            {
                var fromFile = File.ReadAllText(TokenFilePath).Trim();
                if (!string.IsNullOrEmpty(fromFile))
                {
                    return fromFile;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Unreadable token file — fall through to the other sources.
            }
        }

        var fromEnv = Environment.GetEnvironmentVariable("CLAUDE_CODE_OAUTH_TOKEN");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude",
            ".credentials.json");
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("claudeAiOauth", out var oauth) &&
                oauth.TryGetProperty("accessToken", out var token) &&
                token.ValueKind == JsonValueKind.String)
            {
                var value = token.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Unreadable/corrupt credentials file — treat as "not logged in".
            return null;
        }

        return null;
    }
}
