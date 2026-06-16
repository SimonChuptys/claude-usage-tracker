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

        // Swap StubUsageProvider for a real implementation once the data
        // source is wired up (see CLAUDE.md "Usage data source").
        IUsageProvider usageProvider = new StubUsageProvider();

        using var context = new TrayApplicationContext(usageProvider);
        Application.Run(context);
    }
}
