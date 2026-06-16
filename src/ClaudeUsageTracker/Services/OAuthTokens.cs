using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeUsageTracker.Services;

/// <summary>
/// OAuth tokens obtained from the in-app Claude sign-in, persisted to
/// <c>%APPDATA%\ClaudeUsageTracker\tokens.json</c> so the user only signs in once.
/// </summary>
internal sealed record OAuthTokens(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("expires_at")] DateTimeOffset? ExpiresAt)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClaudeUsageTracker",
        "tokens.json");

    /// <summary>True if the access token is missing or within <paramref name="margin"/> of expiry.</summary>
    public bool IsExpired(TimeSpan margin) =>
        ExpiresAt is { } e && DateTimeOffset.UtcNow >= e - margin;

    public static OAuthTokens? Load()
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<OAuthTokens>(File.ReadAllText(FilePath));
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
