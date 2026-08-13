using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class MenuLayoutTests
{
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(960, 540)]
    [InlineData(640, 360)]
    [InlineData(320, 180)]
    public void Calculate_StaysInsideViewport(int viewportWidth, int viewportHeight)
    {
        MenuLayout layout = MenuLayout.Calculate(viewportWidth, viewportHeight);

        Assert.True(layout.X >= 0);
        Assert.True(layout.Y >= 0);
        Assert.True(layout.X + layout.Width <= viewportWidth);
        Assert.True(layout.Y + layout.Height <= viewportHeight);
        Assert.True(layout.ContentTop < layout.ContentBottom);
        Assert.Equal(layout.Y + layout.HeaderHeight + MenuVisualMetrics.CategoryTopSpacing,
            layout.CategoryTop);
        Assert.True(layout.OptionHeight > 0);
        Assert.True(layout.VisibleOptionCount > 0);
    }

    [Fact]
    public void Calculate_RejectsInvalidDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MenuLayout.Calculate(0, 720));
        Assert.Throws<ArgumentOutOfRangeException>(() => MenuLayout.Calculate(1280, 0));
    }

    [Fact]
    public void Calculate_ShowsFewerRowsInSplitScreenViewport()
    {
        MenuLayout fullScreen = MenuLayout.Calculate(1920, 1080);
        MenuLayout splitScreen = MenuLayout.Calculate(640, 360);

        Assert.True(fullScreen.VisibleOptionCount > splitScreen.VisibleOptionCount);
        Assert.Equal(3, splitScreen.VisibleOptionCount);
    }
}
