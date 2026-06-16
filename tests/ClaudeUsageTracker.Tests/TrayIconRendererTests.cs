using System.Drawing;
using Xunit;

namespace ClaudeUsageTracker.Tests;

public class TrayIconRendererTests
{
    private static readonly Color Green = Color.FromArgb(76, 175, 80);
    private static readonly Color Amber = Color.FromArgb(255, 193, 7);
    private static readonly Color Red = Color.FromArgb(244, 67, 54);

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.49)]
    public void ColorFor_is_green_below_half(double used)
    {
        Assert.Equal(Green, TrayIconRenderer.ColorFor(used));
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
}
