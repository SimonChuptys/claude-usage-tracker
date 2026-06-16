using ClaudeUsageTracker.Services;

namespace ClaudeUsageTracker;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application. Runs as a tray-only app
    ///  (no main window) via <see cref="TrayApplicationContext"/>.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Real provider: reads Claude Code's subscription usage via its OAuth
        // login. Use StubUsageProvider instead for offline UI development.
        IUsageProvider usageProvider = new OAuthUsageProvider();

        using var context = new TrayApplicationContext(usageProvider);
        Application.Run(context);
    }
}
