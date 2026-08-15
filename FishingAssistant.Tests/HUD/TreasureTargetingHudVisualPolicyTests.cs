using FishingAssistant.HUD;

namespace FishingAssistant.Tests.HUD;

public sealed class TreasureTargetingHudVisualPolicyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ShouldDraw_ReflectsTargetingState(bool isEnabled, bool expected)
    {
        Assert.Equal(expected, TreasureTargetingHudVisualPolicy.ShouldDraw(isEnabled));
    }
}
