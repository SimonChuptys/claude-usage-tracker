using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ClaudeUsageTracker;

/// <summary>
/// Draws a tray icon showing two stacked horizontal progress bars — the session
/// (5h) limit on top and the weekly limit below — that fill as usage grows, with
/// the most-constrained percentage above them. Each bar's colour shifts from
/// green (little used) to red (nearly exhausted). Rendered at a larger-than-
/// display size so it stays crisp when Windows scales it for high-DPI taskbars.
/// </summary>
internal static class TrayIconRenderer
{
    // Render larger than the 16px logical tray size so the icon stays crisp when
    // Windows scales it up for high-DPI taskbars.
    private const int Size = 32;

    /// <summary>
    /// Renders an icon for the given session and weekly used fractions (each
    /// 0.0–1.0). The caller owns the returned <see cref="Icon"/> and must
    /// dispose it.
    /// </summary>
    public static Icon Render(double sessionFraction, double weeklyFraction)
    {
        sessionFraction = Math.Clamp(sessionFraction, 0.0, 1.0);
        weeklyFraction = Math.Clamp(weeklyFraction, 0.0, 1.0);

        using var bitmap = new Bitmap(Size, Size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.Transparent);

            // Two stacked progress bars along the bottom: session (upper) and
            // weekly (lower). A bit thinner than a single bar so both fit.
            var barHeight = Size * 0.15f; // ~4.8px at 32px
            var barGap = Size / 16f;      // 1px (logical) between the two bars
            var weeklyTop = Size - barHeight;
            var sessionTop = weeklyTop - barGap - barHeight;
            DrawBar(g, sessionTop, barHeight, sessionFraction);
            DrawBar(g, weeklyTop, barHeight, weeklyFraction);

            // Headline percentage = the most-constrained (worst) of the two,
            // filling the area above the bars with a 2px (logical) gap.
            var headline = Math.Max(sessionFraction, weeklyFraction);
            var gap = Size / 16f * 2f;
            var textAreaHeight = sessionTop - gap;
            DrawPercentLabel(g, headline, textAreaHeight);
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

    // Draws one full-width progress bar: a light-grey track plus a coloured fill
    // proportional to the used fraction.
    private static void DrawBar(Graphics g, float top, float height, double fraction)
    {
        using (var track = new SolidBrush(Color.FromArgb(200, 200, 200)))
        {
            g.FillRectangle(track, new RectangleF(0, top, Size, height));
        }
        var fillWidth = (float)(Size * fraction);
        if (fillWidth > 0)
        {
            using var fill = new SolidBrush(ColorFor(fraction));
            g.FillRectangle(fill, new RectangleF(0, top, fillWidth, height));
        }
    }

    // Draws the percentage filling the given area at the top, in white, sized to
    // the largest that fits the area (by the glyphs' *actual* bounds, not the
    // font line box) in both width and height. The em size is computed up front
    // so the path is built at the final size — no post-scaling of geometry.
    private static void DrawPercentLabel(Graphics g, double fraction, float areaHeight)
    {
        var label = FormatPercentLabel(fraction);
        using var family = new FontFamily("Segoe UI");

        // Glyph bounds scale linearly with em size, so measure once at a
        // reference em, then derive the em that exactly fills the area.
        const float referenceEm = 100f;
        RectangleF reference;
        using (var measure = new GraphicsPath())
        {
            measure.AddString(label, family, (int)FontStyle.Bold, referenceEm,
                new PointF(0, 0), StringFormat.GenericTypographic);
            reference = measure.GetBounds();
        }
        var scale = Math.Min(Size / reference.Width, areaHeight / reference.Height);
        var emSize = referenceEm * scale;

        // The glyph bounds at this em are reference*scale; offset the layout
        // origin so those bounds are centred in the area (translation only).
        var originX = (Size - reference.Width * scale) / 2f - reference.X * scale;
        var originY = (areaHeight - reference.Height * scale) / 2f - reference.Y * scale;

        using var path = new GraphicsPath();
        path.AddString(label, family, (int)FontStyle.Bold, emSize,
            new PointF(originX, originY), StringFormat.GenericTypographic);
        using var brush = new SolidBrush(Color.White);
        g.FillPath(brush, path);
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
        >= 0.8 => Color.FromArgb(208, 59, 59),   // red
        >= 0.5 => Color.FromArgb(255, 193, 7),   // amber
        _ => Color.FromArgb(42, 120, 214),       // blue
    };
}
