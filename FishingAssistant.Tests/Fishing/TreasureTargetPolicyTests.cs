using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class TreasureTargetPolicyTests
{
    private static TreasureTargetConditions AvailableTreasure => new(
        true, true, true, false, 1f, 0.9f, false, 200f, 400f);

    [Fact]
    public void Decide_TargetsVisibleTreasureWhenFishProgressIsSafe()
    {
        TreasureTargetDecision decision = TreasureTargetPolicy.Decide(AvailableTreasure);

        Assert.Equal(MinigameTarget.Treasure, decision.Target);
        Assert.Equal(400f, decision.Position);
        Assert.True(decision.IsTargetingTreasure);
    }

    [Fact]
    public void Decide_ContinuesTreasureTargetWithHysteresis()
    {
        TreasureTargetConditions conditions = AvailableTreasure with
        {
            CatchProgress = 0.5f,
            WasTargetingTreasure = true
        };

        Assert.Equal(MinigameTarget.Treasure, TreasureTargetPolicy.Decide(conditions).Target);
    }

    [Fact]
    public void Decide_ReturnsToFishWhenCatchProgressBecomesDangerous()
    {
        TreasureTargetConditions conditions = AvailableTreasure with
        {
            CatchProgress = 0.35f,
            WasTargetingTreasure = true
        };

        TreasureTargetDecision decision = TreasureTargetPolicy.Decide(conditions);

        Assert.Equal(MinigameTarget.Fish, decision.Target);
        Assert.Equal(200f, decision.Position);
        Assert.False(decision.IsTargetingTreasure);
    }

    [Theory]
    [InlineData(false, true, true, false, 1f)]
    [InlineData(true, false, true, false, 1f)]
    [InlineData(true, true, false, false, 1f)]
    [InlineData(true, true, true, true, 1f)]
    [InlineData(true, true, true, false, 0.9f)]
    public void Decide_TargetsFishWhenTreasureIsUnavailable(
        bool assistanceActive,
        bool targetingEnabled,
        bool treasureAvailable,
        bool treasureCaught,
        float treasureScale)
    {
        TreasureTargetConditions conditions = AvailableTreasure with
        {
            AssistanceActive = assistanceActive,
            TreasureTargetingEnabled = targetingEnabled,
            TreasureAvailable = treasureAvailable,
            TreasureCaught = treasureCaught,
            TreasureScale = treasureScale
        };

        Assert.Equal(MinigameTarget.Fish, TreasureTargetPolicy.Decide(conditions).Target);
    }
}
