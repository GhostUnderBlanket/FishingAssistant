using FishingAssistant.UI.Controls;

namespace FishingAssistant.Tests.UI;

public sealed class MouseWheelAdjustmentTests
{
    [Theory]
    [InlineData(120, 1)]
    [InlineData(1, 1)]
    [InlineData(-120, -1)]
    [InlineData(-1, -1)]
    public void GetDirection_UsesNaturalWheelDirection(int wheelDelta, int expected)
    {
        Assert.Equal(expected, MouseWheelAdjustment.GetDirection(wheelDelta));
    }

    [Fact]
    public void GetDirection_IgnoresZeroDelta()
    {
        Assert.Equal(0, MouseWheelAdjustment.GetDirection(0));
    }
}
