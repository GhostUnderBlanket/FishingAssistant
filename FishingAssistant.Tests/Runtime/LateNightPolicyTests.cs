using FishingAssistant.Configuration;
using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Runtime;

public sealed class LateNightPolicyTests
{
    private static LateNightWarningConditions WarningConditions => new(
        PauseFishingBehavior.WarnAndPause,
        true,
        true,
        2400,
        2400,
        0,
        2);

    private static LateNightPauseConditions SafePauseConditions => new(
        true, true, true, true, false, false, false, false, false);

    [Fact]
    public void DecideWarning_WarnsWithoutPausingBeforeLimit()
    {
        Assert.Equal(LateNightWarningAction.Warn,
            LateNightPolicy.DecideWarning(WarningConditions));
    }

    [Fact]
    public void DecideWarning_RequestsPauseOnFinalWarning()
    {
        LateNightWarningConditions conditions = WarningConditions with { WarningsIssued = 1 };

        Assert.Equal(LateNightWarningAction.WarnAndRequestPause,
            LateNightPolicy.DecideWarning(conditions));
    }

    [Fact]
    public void DecideWarning_WarnOnlyNeverRequestsPause()
    {
        LateNightWarningConditions conditions = WarningConditions with
        {
            Behavior = PauseFishingBehavior.WarnOnly,
            WarningsIssued = 1
        };

        Assert.Equal(LateNightWarningAction.Warn,
            LateNightPolicy.DecideWarning(conditions));
    }

    [Theory]
    [InlineData((int)PauseFishingBehavior.Off, true, true, 2400, 0)]
    [InlineData((int)PauseFishingBehavior.WarnAndPause, false, true, 2400, 0)]
    [InlineData((int)PauseFishingBehavior.WarnAndPause, true, false, 2400, 0)]
    [InlineData((int)PauseFishingBehavior.WarnAndPause, true, true, 2350, 0)]
    [InlineData((int)PauseFishingBehavior.WarnAndPause, true, true, 2400, 2)]
    public void DecideWarning_DoesNothingWhenIneligible(
        int behaviorValue,
        bool enabled,
        bool fishingContext,
        int currentTime,
        int warningsIssued)
    {
        LateNightWarningConditions conditions = WarningConditions with
        {
            Behavior = (PauseFishingBehavior)behaviorValue,
            AutomationEnabled = enabled,
            IsFishingContext = fishingContext,
            CurrentTime = currentTime,
            WarningsIssued = warningsIssued
        };

        Assert.Equal(LateNightWarningAction.None,
            LateNightPolicy.DecideWarning(conditions));
    }

    [Fact]
    public void ShouldPause_WhenPendingAndFishingCycleIsIdle()
    {
        Assert.True(LateNightPolicy.ShouldPause(SafePauseConditions));
    }

    [Theory]
    [InlineData(true, false, false, false, false)]
    [InlineData(false, true, false, false, false)]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(false, false, false, false, true)]
    public void ShouldPause_WaitsForUnsafeFishingContextToFinish(
        bool rodInUse,
        bool menu,
        bool minigame,
        bool gameEvent,
        bool festival)
    {
        LateNightPauseConditions conditions = SafePauseConditions with
        {
            IsRodInUse = rodInUse,
            HasBlockingMenu = menu,
            HasMinigame = minigame,
            IsEvent = gameEvent,
            IsFestival = festival
        };

        Assert.False(LateNightPolicy.ShouldPause(conditions));
    }
}
