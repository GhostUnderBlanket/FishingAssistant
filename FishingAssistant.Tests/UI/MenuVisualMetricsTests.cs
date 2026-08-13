using FishingAssistant.UI;
using Microsoft.Xna.Framework;

namespace FishingAssistant.Tests.UI;

public sealed class MenuVisualMetricsTests
{
    [Fact]
    public void ControlMetrics_KeepSelectorsAndActionsConsistent()
    {
        Assert.Equal(48, MenuVisualMetrics.GetControlHeight(52));
        Assert.Equal(320, MenuVisualMetrics.GetControlWidth(800));
        Assert.Equal(1.75f, MenuVisualMetrics.ArrowScale);
        Assert.Equal(20, MenuVisualMetrics.HeaderPanelHorizontalPadding);
        Assert.Equal(12, MenuVisualMetrics.HeaderPanelVerticalPadding);
        Assert.Equal(12, MenuVisualMetrics.ScrollbarGap);
        Assert.Equal(8, MenuVisualMetrics.ScrollbarVerticalInset);
        Assert.Equal(8, MenuVisualMetrics.CategoryTopSpacing);
    }

    [Fact]
    public void ControlMetrics_ShrinkWithinSmallRows()
    {
        Assert.Equal(36, MenuVisualMetrics.GetControlHeight(40));
        Assert.Equal(152, MenuVisualMetrics.GetControlWidth(300));
    }

    [Fact]
    public void InlineMessagePalette_UsesDarkTextOnALightBackground()
    {
        Color text = MenuVisualMetrics.InlineMessageText;
        Color background = MenuVisualMetrics.InlineMessageBackground;

        int textBrightness = text.R + text.G + text.B;
        int backgroundBrightness = background.R + background.G + background.B;

        Assert.True(backgroundBrightness - textBrightness >= 400);
        Assert.NotEqual(MenuVisualMetrics.InlineMessageAccent, text);
    }
}
