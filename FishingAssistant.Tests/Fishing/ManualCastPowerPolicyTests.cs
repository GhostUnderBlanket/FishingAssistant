using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class ManualCastPowerPolicyTests
{
    [Fact]
    public void Decide_HoldsConfiguredPowerUntilThreshold()
    {
        Assert.Equal(ManualCastPowerDecision.HoldConfiguredPower,
            ManualCastPowerPolicy.Decide(true, false, 59, 1f));
        Assert.Equal(ManualCastPowerDecision.UseVanilla,
            ManualCastPowerPolicy.Decide(true, false, 60, 1f));
    }

    [Fact]
    public void Decide_UnlocksImmediatelyAtZero()
    {
        Assert.Equal(ManualCastPowerDecision.UseVanilla,
            ManualCastPowerPolicy.Decide(true, false, 0, 0f));
    }

    [Fact]
    public void Decide_NeverUnlocksAtThreeSeconds()
    {
        Assert.Equal(ManualCastPowerDecision.HoldConfiguredPower,
            ManualCastPowerPolicy.Decide(true, false, 60_000, 3f));
    }

    [Fact]
    public void Decide_DoesNotInterfereWithAutomaticCast()
    {
        Assert.Equal(ManualCastPowerDecision.Reset,
            ManualCastPowerPolicy.Decide(true, true, 0, 1f));
    }

    [Fact]
    public void Decide_ResetsOutsideCastTiming()
    {
        Assert.Equal(ManualCastPowerDecision.Reset,
            ManualCastPowerPolicy.Decide(false, false, 30, 1f));
    }
}
