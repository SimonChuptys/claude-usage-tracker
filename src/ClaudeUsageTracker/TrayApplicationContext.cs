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
    // The usage endpoint rate-limits aggressively (safe at ~180s); poll every
    // 5 minutes to stay well clear.
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    private readonly IUsageProvider _usageProvider;
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private Icon? _currentIcon;
    private bool _signingIn;
    private bool _authBalloonShown;

    public TrayApplicationContext(IUsageProvider usageProvider)
    {
        _usageProvider = usageProvider;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Sign in to Claude…", null, async (_, _) => await SignInAsync());
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
        // The error balloon is the "action button": clicking it starts sign-in.
        _notifyIcon.BalloonTipClicked += async (_, _) => await SignInAsync();

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
        catch (AuthRequiredException ex)
        {
            SetIcon(TrayIconRenderer.RenderError());
            _notifyIcon.Text = Truncate($"Claude usage — {ex.Message}");
            // Prompt to sign in via a clickable balloon, once per auth-required
            // spell (reset after the next successful refresh) to avoid nagging.
            if (!_authBalloonShown)
            {
                _authBalloonShown = true;
                _notifyIcon.ShowBalloonTip(
                    10_000, "Claude Usage Tracker",
                    $"{ex.Message} — click here to sign in.", ToolTipIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            SetIcon(TrayIconRenderer.RenderError());
            _notifyIcon.Text = Truncate($"Claude usage — error: {ex.Message}");
        }
    }

    // Runs the interactive OAuth sign-in (opens a browser), saves the tokens, and
    // refreshes. Guarded against re-entrancy so double-clicks don't open two flows.
    private async Task SignInAsync()
    {
        if (_signingIn)
        {
            return;
        }
        _signingIn = true;
        try
        {
            // The Claude login page shows "Claude Code" (we reuse its OAuth
            // client to read usage) — flag that so it isn't a surprise.
            _notifyIcon.ShowBalloonTip(
                10_000, "Claude Usage Tracker",
                "Opening your browser to sign in to Claude. The page will show " +
                "\"Claude Code\" — that's expected; the tracker reuses Claude " +
                "Code's login to read your usage.", ToolTipIcon.Info);

            var tokens = await ClaudeOAuthLogin.LoginAsync();
            tokens.Save();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            SetIcon(TrayIconRenderer.RenderError());
            _notifyIcon.Text = Truncate($"Claude usage — sign-in failed: {ex.Message}");
        }
        finally
        {
            _signingIn = false;
        }
    }

    private void UpdateUi(UsageSnapshot snapshot)
    {
        _authBalloonShown = false;

        var constrained = snapshot.MostConstrained;
        var used = constrained?.UsedFraction ?? 0.0;

        SetIcon(TrayIconRenderer.Render(used));

        // NotifyIcon.Text is limited to 127 characters.
        var lines = snapshot.Limits.Select(l =>
        {
            var resets = l.ResetsAt is { } r ? $" · resets {r.LocalDateTime:t}" : string.Empty;
            var weeklySpacing = l.Name.StartsWith("Weekly") ? "        " : string.Empty;
            
            return $"{l.Name}:   {weeklySpacing}{l.UsedPercent}% used{resets}";
        });
        // NotifyIcon tooltips are plain OS strings with no formatting, so we fake
        // a bold heading using Unicode "mathematical sans-serif bold" letters,
        // which Segoe UI renders bold.
        _notifyIcon.Text = Truncate("𝗖𝗹𝗮𝘂𝗱𝗲 𝘂𝘀𝗮𝗴𝗲\n" + string.Join("\n", lines));
    }

    // Swaps in a freshly rendered icon and disposes the previous one to avoid
    // leaking GDI icon handles.
    private void SetIcon(Icon newIcon)
    {
        _notifyIcon.Icon = newIcon;
        _currentIcon?.Dispose();
        _currentIcon = newIcon;
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
