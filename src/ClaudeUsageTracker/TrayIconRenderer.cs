using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ClaudeUsageTracker;

/// <summary>
/// Draws a tray icon showing the used-usage percentage as a full-width
/// horizontal progress bar that fills as usage grows, with the percentage
/// number above it. The bar's colour shifts from green (little used) to red
/// (nearly exhausted). Rendered at a larger-than-display size so it stays crisp
/// when Windows scales it for high-DPI taskbars.
/// </summary>
internal static class TrayIconRenderer
{
    // Render larger than the 16px logical tray size so the icon stays crisp when
    // Windows scales it up for high-DPI taskbars.
    private const int Size = 32;

    /// <summary>
    /// Renders an icon for the given used fraction (0.0–1.0). The caller owns
    /// the returned <see cref="Icon"/> and must dispose it.
    /// </summary>
    public static Icon Render(double usedFraction)
    {
        usedFraction = Math.Clamp(usedFraction, 0.0, 1.0);

        using var bitmap = new Bitmap(Size, Size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            var color = ColorFor(usedFraction);

            // Full-width horizontal progress bar along the bottom: a light-grey
            // track plus a coloured fill proportional to the used fraction.
            var barHeight = Size * 0.22f; // ~7px at 32px
            var barTop = Size - barHeight;
            using (var track = new SolidBrush(Color.FromArgb(200, 200, 200)))
            {
                g.FillRectangle(track, new RectangleF(0, barTop, Size, barHeight));
            }
            var fillWidth = (float)(Size * usedFraction);
            if (fillWidth > 0)
            {
                using var fill = new SolidBrush(color);
                g.FillRectangle(fill, new RectangleF(0, barTop, fillWidth, barHeight));
            }

            // Full percentage filling the whole area above the bar (e.g. "83",
            // "100"), with a 2px (logical) gap above the bar. Build the glyphs as
            // a path and scale their *actual* bounds (not the font line box) to
            // the largest size that fits that area in both width and height.
            var gap = Size / 16f * 2f; // 1px at the 16px logical tray size
            var textAreaHeight = barTop - gap;
            var label = FormatPercentLabel(usedFraction);

            using var path = new GraphicsPath();
            using (var family = new FontFamily("Segoe UI"))
            {
                // emSize is arbitrary here; the path is rescaled to fit below.
                path.AddString(label, family, (int)FontStyle.Bold, 100f,
                    new PointF(0, 0), StringFormat.GenericTypographic);
            }
            var bounds = path.GetBounds();
            var scale = Math.Min(Size / bounds.Width, textAreaHeight / bounds.Height);
            using (var transform = new Matrix())
            {
                // Move glyph bounds to the origin, scale up, then centre in the
                // box above the bar.
                transform.Translate((Size - bounds.Width * scale) / 2f,
                    (textAreaHeight - bounds.Height * scale) / 2f);
                transform.Scale(scale, scale);
                transform.Translate(-bounds.X, -bounds.Y);
                path.Transform(transform);
            }
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillPath(brush, path);
            }
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

            var rect = new RectangleF(1, 1, Size - 3, Size - 3);
            using (var fill = new SolidBrush(red))
            {
                g.FillEllipse(fill, rect);
            }

            using var font = new Font("Segoe UI", Size * 0.6f, FontStyle.Bold, GraphicsUnit.Pixel);
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

    /// <summary>
    /// Formats a used fraction (0.0–1.0) as a whole-percent label for the icon
    /// centre: "0".."99" and "100" (clamped to that range).
    /// </summary>
    internal static string FormatPercentLabel(double usedFraction)
    {
        var percent = (int)Math.Round(Math.Clamp(usedFraction, 0.0, 1.0) * 100);
        return percent.ToString();
    }

    internal static Color ColorFor(double used) => used switch
    {
        >= 0.8 => Color.FromArgb(244, 67, 54),   // red
        >= 0.5 => Color.FromArgb(255, 193, 7),   // amber
        _ => Color.FromArgb(76, 175, 80),         // green
    };
}
