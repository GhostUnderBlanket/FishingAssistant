using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class MenuVisualMetricsTests
{
    [Fact]
    public void ControlMetrics_KeepSelectorsAndActionsConsistent()
    {
        Assert.Equal(48, MenuVisualMetrics.GetControlHeight(52));
        Assert.Equal(320, MenuVisualMetrics.GetControlWidth(800));
        Assert.Equal(1.75f, MenuVisualMetrics.ArrowScale);
    }

    [Fact]
    public void ControlMetrics_ShrinkWithinSmallRows()
    {
        Assert.Equal(36, MenuVisualMetrics.GetControlHeight(40));
        Assert.Equal(152, MenuVisualMetrics.GetControlWidth(300));
    }
}
