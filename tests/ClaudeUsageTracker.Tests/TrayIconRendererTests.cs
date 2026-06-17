using System.Drawing;
using Xunit;

namespace ClaudeUsageTracker.Tests;

public class TrayIconRendererTests
{
    private static readonly Color Blue = Color.FromArgb(42, 120, 214);
    private static readonly Color Amber = Color.FromArgb(255, 193, 7);
    private static readonly Color Red = Color.FromArgb(208, 59, 59);

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.49)]
    public void ColorFor_is_blue_below_half(double used)
    {
        Assert.Equal(Blue, TrayIconRenderer.ColorFor(used));
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.79)]
    public void ColorFor_is_amber_from_half_to_eighty(double used)
    {
        Assert.Equal(Amber, TrayIconRenderer.ColorFor(used));
    }

    [Theory]
    [InlineData(0.8)]
    [InlineData(1.0)]
    public void ColorFor_is_red_from_eighty(double used)
    {
        Assert.Equal(Red, TrayIconRenderer.ColorFor(used));
    }

    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(0.07, "7")]
    [InlineData(0.83, "83")]
    [InlineData(1.0, "100")]
    [InlineData(-0.1, "0")]
    [InlineData(1.5, "100")]
    public void FormatPercentLabel_shows_clamped_whole_percent(double used, string expected)
    {
        Assert.Equal(expected, TrayIconRenderer.FormatPercentLabel(used));
    }
}
