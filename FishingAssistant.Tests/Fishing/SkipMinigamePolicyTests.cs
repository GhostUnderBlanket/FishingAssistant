using FishingAssistant.Configuration;
using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class SkipMinigamePolicyTests
{
    private static SkipMinigameConditions SafeConditions => new(
        SkipMinigameBehavior.SkipAll, true, false, false, false);

    [Fact]
    public void Decide_SkipsEveryFishForSkipAll()
    {
        Assert.Equal(SkipMinigameDecision.Skip, SkipMinigamePolicy.Decide(SafeConditions));
    }

    [Theory]
    [InlineData(false, (int)SkipMinigameDecision.Play)]
    [InlineData(true, (int)SkipMinigameDecision.Skip)]
    public void Decide_SkipOnlyCaughtRequiresPreviousCatch(bool caughtBefore, int expectedValue)
    {
        SkipMinigameConditions conditions = SafeConditions with
        {
            Behavior = SkipMinigameBehavior.SkipOnlyCaught,
            FishWasCaughtBefore = caughtBefore
        };

        Assert.Equal((SkipMinigameDecision)expectedValue, SkipMinigamePolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_PlaysWhenSkippingIsOff()
    {
        SkipMinigameConditions conditions = SafeConditions with { Behavior = SkipMinigameBehavior.Off };

        Assert.Equal(SkipMinigameDecision.Play, SkipMinigamePolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_PlaysWhileMinigameIsStartingOrEnding()
    {
        SkipMinigameConditions conditions = SafeConditions with { IsMinigameActive = false };

        Assert.Equal(SkipMinigameDecision.Play, SkipMinigamePolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_AllowsSupportedFestivalFishingMinigame()
    {
        SkipMinigameConditions conditions = SafeConditions with
        {
            IsFestival = true,
            IsSupportedFishingMinigame = true
        };

        Assert.Equal(SkipMinigameDecision.Skip, SkipMinigamePolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_BlocksUnrelatedFestivalContext()
    {
        SkipMinigameConditions conditions = SafeConditions with { IsFestival = true };

        Assert.Equal(SkipMinigameDecision.Play, SkipMinigamePolicy.Decide(conditions));
    }
}
