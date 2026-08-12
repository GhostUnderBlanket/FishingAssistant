using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class FishDifficultyPolicyTests
{
    [Fact]
    public void Decide_AppliesMultiplierBeforeAdditive()
    {
        FishDifficultyDecision decision = FishDifficultyPolicy.Decide(new(80f, 0.5f, 10));

        Assert.Equal(50f, decision.AdjustedDifficulty);
        Assert.True(decision.WasChanged);
    }

    [Fact]
    public void Decide_DefaultSettingsPreserveVanillaDifficulty()
    {
        FishDifficultyDecision decision = FishDifficultyPolicy.Decide(new(65f, 1f, 0));

        Assert.Equal(65f, decision.AdjustedDifficulty);
        Assert.False(decision.WasChanged);
    }

    [Theory]
    [InlineData(20f, 0f, 0, 0f)]
    [InlineData(20f, 1f, -100, 0f)]
    [InlineData(20f, 2f, -10, 30f)]
    public void Decide_ClampsOnlyTheLowerBound(
        float vanilla,
        float multiplier,
        int additive,
        float expected)
    {
        Assert.Equal(expected,
            FishDifficultyPolicy.Decide(new(vanilla, multiplier, additive)).AdjustedDifficulty);
    }

    [Fact]
    public void Decide_PreservesLargeConfiguredDifficultyForLegacyRange()
    {
        FishDifficultyDecision decision = FishDifficultyPolicy.Decide(new(110f, 10f, 100));

        Assert.Equal(1200f, decision.AdjustedDifficulty);
    }

    [Theory]
    [InlineData(float.NaN, 1f, 0, 0f)]
    [InlineData(50f, float.NaN, 0, 50f)]
    [InlineData(50f, float.PositiveInfinity, 0, 50f)]
    public void Decide_RecoversFromNonFiniteInput(
        float vanilla,
        float multiplier,
        int additive,
        float expected)
    {
        Assert.Equal(expected,
            FishDifficultyPolicy.Decide(new(vanilla, multiplier, additive)).AdjustedDifficulty);
    }
}
