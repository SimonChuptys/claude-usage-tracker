using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClaudeUsageTracker.Services;

/// <summary>
/// Implements Claude Code's OAuth 2.0 PKCE login entirely in-app: opens the
/// system browser to the Anthropic authorize page, captures the redirect on a
/// localhost loopback socket, and exchanges the code for tokens. Also refreshes
/// an expired access token using a stored refresh token.
/// </summary>
/// <remarks>
/// Uses the (undocumented, public) Claude Code OAuth client and endpoints — the
/// same flow the CLI's <c>/login</c> uses. May change without notice.
/// </remarks>
internal static class ClaudeOAuthLogin
{
    private const string ClientId = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
    private const string AuthorizeUrl = "https://claude.ai/oauth/authorize";
    private const string TokenUrl = "https://platform.claude.com/v1/oauth/token";
    private const string Scopes = "user:profile user:inference user:sessions:claude_code user:mcp_servers";

    private static readonly HttpClient Http = new();

    /// <summary>
    /// Runs the full interactive login and returns the resulting tokens. Opens a
    /// browser; completes when the user approves (or throws on cancel/timeout).
    /// </summary>
    public static async Task<OAuthTokens> LoginAsync(CancellationToken cancellationToken = default)
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(32));

        // Loopback socket on an OS-assigned free port (RFC 8252 native-app flow).
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var redirectUri = $"http://localhost:{port}/callback";

        var authorizeUrl =
            $"{AuthorizeUrl}?response_type=code" +
            $"&client_id={Uri.EscapeDataString(ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&scope={Uri.EscapeDataString(Scopes)}" +
            $"&code_challenge={challenge}&code_challenge_method=S256" +
            $"&state={Uri.EscapeDataString(state)}";

        Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true });

        var (code, returnedState) = await ReceiveRedirectAsync(listener, cancellationToken);
        if (returnedState != state)
        {
            throw new InvalidOperationException("Sign-in failed: state mismatch");
        }

        return await ExchangeAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = ClientId,
            ["code_verifier"] = verifier,
            ["state"] = state,
        }, cancellationToken);
    }

    /// <summary>Exchanges a refresh token for a fresh access token.</summary>
    public static Task<OAuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        ExchangeAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = ClientId,
        }, cancellationToken);

    private static async Task<OAuthTokens> ExchangeAsync(
        Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await Http.PostAsync(TokenUrl, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Token request failed ({(int)response.StatusCode})");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Token response had no access_token");
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        DateTimeOffset? expiresAt = root.TryGetProperty("expires_in", out var exp) && exp.TryGetInt32(out var secs)
            ? DateTimeOffset.UtcNow.AddSeconds(secs)
            : null;

        return new OAuthTokens(accessToken, refreshToken, expiresAt);
    }

    // Accepts a single loopback connection, parses code/state from the GET line,
    // and returns a friendly HTML page to the browser.
    private static async Task<(string Code, string State)> ReceiveRedirectAsync(
        TcpListener listener, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));

        using var client = await listener.AcceptTcpClientAsync(timeout.Token);
        using var stream = client.GetStream();

        var buffer = new byte[4096];
        var read = await stream.ReadAsync(buffer, timeout.Token);
        var requestLine = Encoding.ASCII.GetString(buffer, 0, read).Split("\r\n")[0];
        // "GET /callback?code=...&state=... HTTP/1.1"
        var target = requestLine.Split(' ').ElementAtOrDefault(1) ?? string.Empty;
        var query = ParseQuery(target);

        var responseBody = "<html><body style='font-family:sans-serif'>"
            + "<h3>Signed in to Claude</h3><p>You can close this tab and return to Claude Usage Tracker.</p>"
            + "</body></html>";
        var response = "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nConnection: close\r\n"
            + $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n\r\n{responseBody}";
        var responseBytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(responseBytes, timeout.Token);

        query.TryGetValue("code", out var code);
        query.TryGetValue("state", out var state);
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            var error = query.TryGetValue("error", out var e) ? e : "no authorization code returned";
            throw new InvalidOperationException($"Sign-in failed: {error}");
        }
        return (code, state);
    }

    private static Dictionary<string, string> ParseQuery(string target)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var q = target.IndexOf('?');
        if (q < 0)
        {
            return result;
        }
        foreach (var pair in target[(q + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            var key = eq < 0 ? pair : pair[..eq];
            var value = eq < 0 ? string.Empty : pair[(eq + 1)..];
            result[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value);
        }
        return result;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
