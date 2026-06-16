namespace ClaudeUsageTracker.Services;

/// <summary>
/// Thrown when no usable Claude token is available (none stored, or the stored
/// session could not be refreshed). Signals the tray to prompt the user to sign
/// in rather than showing a generic error.
/// </summary>
public sealed class AuthRequiredException : Exception
{
    public AuthRequiredException(string message) : base(message) { }
}
