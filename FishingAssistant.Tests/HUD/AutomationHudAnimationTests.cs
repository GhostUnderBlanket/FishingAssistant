using FishingAssistant.HUD;

namespace FishingAssistant.Tests.HUD;

public sealed class AutomationHudAnimationTests
{
    [Theory]
    [InlineData(0, 24)]
    [InlineData(249, 24)]
    [InlineData(250, 25)]
    [InlineData(750, 27)]
    [InlineData(1000, 24)]
    public void GetEmoteFrame_CyclesVanillaEmoteFrames(int elapsedMilliseconds, int expected)
    {
        Assert.Equal(expected, AutomationHudAnimation.GetEmoteFrame(24, elapsedMilliseconds));
    }
}
