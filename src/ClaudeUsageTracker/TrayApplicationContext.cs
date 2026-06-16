using ClaudeUsageTracker.Models;
using ClaudeUsageTracker.Services;

namespace ClaudeUsageTracker;

/// <summary>
/// Hosts the tray (notification area) icon, its context menu, and a timer that
/// periodically refreshes the usage data. This is the application's UI: there
/// is no main window.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);

    private readonly IUsageProvider _usageProvider;
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private Icon? _currentIcon;

    public TrayApplicationContext(IUsageProvider usageProvider)
    {
        _usageProvider = usageProvider;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Refresh now", null, async (_, _) => await RefreshAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _notifyIcon = new NotifyIcon
        {
            Text = "Claude usage — loading…",
            Visible = true,
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
        };

        _refreshTimer = new System.Windows.Forms.Timer { Interval = (int)RefreshInterval.TotalMilliseconds };
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();
        _refreshTimer.Start();

        // Kick off the first load immediately.
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var snapshot = await _usageProvider.GetUsageAsync();
            UpdateUi(snapshot);
        }
        catch (Exception ex)
        {
            _notifyIcon.Text = Truncate($"Claude usage — error: {ex.Message}");
        }
    }

    private void UpdateUi(UsageSnapshot snapshot)
    {
        var constrained = snapshot.MostConstrained;
        var remaining = constrained?.RemainingFraction ?? 1.0;

        var newIcon = TrayIconRenderer.Render(remaining);
        _notifyIcon.Icon = newIcon;
        _currentIcon?.Dispose();
        _currentIcon = newIcon;

        // NotifyIcon.Text is limited to 127 characters.
        var lines = snapshot.Limits.Select(l =>
        {
            var resets = l.ResetsAt is { } r ? $" · resets {r.LocalDateTime:t}" : string.Empty;
            return $"{l.Name}: {l.RemainingPercent}% left{resets}";
        });
        _notifyIcon.Text = Truncate("Claude usage\n" + string.Join("\n", lines));
    }

    private static string Truncate(string text) =>
        text.Length <= 127 ? text : text[..127];

    private void ExitApplication()
    {
        _refreshTimer.Stop();
        _notifyIcon.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Dispose();
            _notifyIcon.Dispose();
            _currentIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
