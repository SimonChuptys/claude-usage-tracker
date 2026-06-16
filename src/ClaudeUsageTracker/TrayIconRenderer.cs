using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ClaudeUsageTracker;

/// <summary>
/// Draws a small 16x16 tray icon showing the remaining-usage percentage as a
/// ring whose colour shifts from green (plenty left) to red (nearly out).
/// </summary>
internal static class TrayIconRenderer
{
    private const int Size = 16;

    /// <summary>
    /// Renders an icon for the given remaining fraction (0.0–1.0). The caller
    /// owns the returned <see cref="Icon"/> and must dispose it.
    /// </summary>
    public static Icon Render(double remainingFraction)
    {
        remainingFraction = Math.Clamp(remainingFraction, 0.0, 1.0);

        using var bitmap = new Bitmap(Size, Size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            var color = ColorFor(remainingFraction);
            var rect = new Rectangle(1, 1, Size - 3, Size - 3);

            // Track ring + filled arc for the remaining portion.
            using (var track = new Pen(Color.FromArgb(60, color), 2))
            {
                g.DrawEllipse(track, rect);
            }
            using (var arc = new Pen(color, 2))
            {
                var sweep = (float)(360.0 * remainingFraction);
                g.DrawArc(arc, rect, -90, sweep);
            }

            // Tens digit of the percentage in the centre (e.g. "8" for 80%+).
            var percent = (int)Math.Round(remainingFraction * 100);
            var label = percent >= 100 ? "F" : (percent / 10).ToString();
            using var font = new Font("Segoe UI", 6.5f, FontStyle.Bold, GraphicsUnit.Point);
            using var brush = new SolidBrush(color);
            var textSize = g.MeasureString(label, font);
            g.DrawString(label, font, brush,
                (Size - textSize.Width) / 2f,
                (Size - textSize.Height) / 2f);
        }

        // Icon.FromHandle does not own the handle; destroy it to avoid a leak.
        var hIcon = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(hIcon);
        }
    }

    /// <summary>
    /// Renders a red error icon (filled disc with a white "!") shown when a
    /// refresh fails. The caller owns the returned <see cref="Icon"/> and must
    /// dispose it.
    /// </summary>
    public static Icon RenderError()
    {
        var red = Color.FromArgb(244, 67, 54);

        using var bitmap = new Bitmap(Size, Size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            var rect = new Rectangle(1, 1, Size - 3, Size - 3);
            using (var fill = new SolidBrush(red))
            {
                g.FillEllipse(fill, rect);
            }

            using var font = new Font("Segoe UI", 8f, FontStyle.Bold, GraphicsUnit.Point);
            using var brush = new SolidBrush(Color.White);
            var textSize = g.MeasureString("!", font);
            g.DrawString("!", font, brush,
                (Size - textSize.Width) / 2f,
                (Size - textSize.Height) / 2f);
        }

        // Icon.FromHandle does not own the handle; destroy it to avoid a leak.
        var hIcon = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(hIcon);
        }
    }

    private static Color ColorFor(double remaining) => remaining switch
    {
        >= 0.5 => Color.FromArgb(76, 175, 80),   // green
        >= 0.2 => Color.FromArgb(255, 193, 7),   // amber
        _ => Color.FromArgb(244, 67, 54),         // red
    };
}
