using FishingAssistant.UI.Controls;

namespace FishingAssistant.Tests.UI;

public sealed class OptionAdjustmentTests
{
    [Fact]
    public void Cycle_WrapsInBothDirections()
    {
        string[] values = ["Off", "Warn", "Pause"];

        Assert.Equal("Off", OptionAdjustment.Cycle(values, "Pause", 1));
        Assert.Equal("Pause", OptionAdjustment.Cycle(values, "Off", -1));
    }

    [Fact]
    public void Step_ClampsAndAvoidsFloatingPointDrift()
    {
        Assert.Equal(1d, OptionAdjustment.Step(0.9d, 1, 0.1d, 0d, 1d));
        Assert.Equal(0d, OptionAdjustment.Step(0d, -1, 0.1d, 0d, 1d));
    }
}
