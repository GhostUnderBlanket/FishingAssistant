using FishingAssistant.Fishing;
using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Fishing;

public sealed class AutoCastPolicyTests
{
    private static AutoCastConditions SafeConditions => new(
        true, true, AutomationState.Ready, true, true, false, true, false, false, true);

    [Fact]
    public void Decide_WaitsUntilConfiguredDelayHasElapsed()
    {
        Assert.Equal(AutoCastDecision.Wait, AutoCastPolicy.Decide(SafeConditions, 58, 60));
        Assert.Equal(AutoCastDecision.Cast, AutoCastPolicy.Decide(SafeConditions, 59, 60));
    }

    [Theory]
    [InlineData(false, true, true, false, true)]
    [InlineData(true, false, true, false, true)]
    [InlineData(true, true, false, false, true)]
    [InlineData(true, true, true, true, true)]
    [InlineData(true, true, true, false, false)]
    public void Decide_ResetsWhenCastingIsUnsafe(
        bool playerFree,
        bool canMove,
        bool enoughStamina,
        bool festival,
        bool fishable)
    {
        AutoCastConditions conditions = SafeConditions with
        {
            IsPlayerFree = playerFree,
            CanMove = canMove,
            HasEnoughStamina = enoughStamina,
            IsFestival = festival,
            IsTargetFishable = fishable
        };

        Assert.Equal(AutoCastDecision.Reset, AutoCastPolicy.Decide(conditions, 59, 60));
    }

    [Fact]
    public void Decide_CastsImmediatelyWhenDelayIsZero()
    {
        Assert.Equal(AutoCastDecision.Cast, AutoCastPolicy.Decide(SafeConditions, 0, 0));
    }

    [Fact]
    public void Decide_AllowsSupportedFestivalFishingMinigame()
    {
        AutoCastConditions conditions = SafeConditions with
        {
            IsPlayerFree = false,
            IsFestival = true,
            IsSupportedFishingMinigame = true
        };

        Assert.Equal(AutoCastDecision.Cast, AutoCastPolicy.Decide(conditions, 59, 60));
    }

    [Fact]
    public void Decide_StillBlocksNonFishingFestivalContext()
    {
        AutoCastConditions conditions = SafeConditions with
        {
            IsFestival = true,
            IsSupportedFishingMinigame = false
        };

        Assert.Equal(AutoCastDecision.Reset, AutoCastPolicy.Decide(conditions, 59, 60));
    }
}
