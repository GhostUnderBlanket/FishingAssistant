using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class ManualCastPowerPolicyTests
{
    private static ManualCastPowerConditions TimingCast => new(
        AutomationEnabled: true,
        IsTimingCast: true,
        WasTimingManualCast: false,
        PlayerInputObserved: false,
        IsAutomaticCast: false,
        WasUnlocked: false,
        ElapsedTicks: 0,
        UnlockSeconds: 1f);

    [Fact]
    public void Decide_HoldsSessionPowerUntilThreshold()
    {
        Assert.Equal(ManualCastPowerDecision.HoldSessionPower,
            ManualCastPowerPolicy.Decide(TimingCast with { ElapsedTicks = 59 }));
        Assert.Equal(ManualCastPowerDecision.UseVanilla,
            ManualCastPowerPolicy.Decide(TimingCast with { ElapsedTicks = 60 }));
    }

    [Fact]
    public void Decide_UnlocksImmediatelyAtZero()
    {
        Assert.Equal(ManualCastPowerDecision.UseVanilla,
            ManualCastPowerPolicy.Decide(TimingCast with { UnlockSeconds = 0f }));
    }

    [Fact]
    public void Decide_NeverUnlocksAtThreeSeconds()
    {
        Assert.Equal(ManualCastPowerDecision.HoldSessionPower,
            ManualCastPowerPolicy.Decide(TimingCast with
            {
                ElapsedTicks = 60_000,
                UnlockSeconds = 3f
            }));
    }

    [Fact]
    public void Decide_RemembersVanillaPowerAfterUnlockedManualCastEnds()
    {
        Assert.Equal(ManualCastPowerDecision.RememberVanillaPower,
            ManualCastPowerPolicy.Decide(TimingCast with
            {
                IsTimingCast = false,
                WasTimingManualCast = true,
                PlayerInputObserved = true,
                WasUnlocked = true
            }));
    }

    [Fact]
    public void Decide_DoesNotRememberPowerWhenReleasedBeforeUnlock()
    {
        Assert.Equal(ManualCastPowerDecision.Reset,
            ManualCastPowerPolicy.Decide(TimingCast with
            {
                IsTimingCast = false,
                WasTimingManualCast = true,
                PlayerInputObserved = true
            }));
    }

    [Fact]
    public void Decide_DoesNotInterfereWithAutomaticCast()
    {
        Assert.Equal(ManualCastPowerDecision.Reset,
            ManualCastPowerPolicy.Decide(TimingCast with { IsAutomaticCast = true }));
    }

    [Fact]
    public void Decide_DoesNotLockOrLearnWhileAutomationIsDisabled()
    {
        Assert.Equal(ManualCastPowerDecision.Reset,
            ManualCastPowerPolicy.Decide(TimingCast with { AutomationEnabled = false }));
    }
}
