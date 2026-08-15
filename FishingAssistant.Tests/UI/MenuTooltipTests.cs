using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class MenuTooltipTests
{
    [Theory]
    [InlineData(1280, 640)]
    [InlineData(800, 640)]
    [InlineData(640, 576)]
    [InlineData(320, 256)]
    [InlineData(32, 1)]
    public void GetTextWidth_RespectsPreferredWidthAndViewport(int viewportWidth, int expected)
    {
        Assert.Equal(expected, MenuTooltip.GetTextWidth(viewportWidth));
    }
}
