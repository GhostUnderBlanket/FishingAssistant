using FishingAssistant.UI.Controls;
using Microsoft.Xna.Framework;

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

    [Fact]
    public void IsPointerOver_OnlyAcceptsTheSelectorBox()
    {
        Rectangle selector = new(480, 100, 280, 48);

        Assert.True(MouseWheelAdjustment.IsPointerOver(selector, new Point(600, 120)));
        Assert.False(MouseWheelAdjustment.IsPointerOver(selector, new Point(200, 120)));
    }
}
