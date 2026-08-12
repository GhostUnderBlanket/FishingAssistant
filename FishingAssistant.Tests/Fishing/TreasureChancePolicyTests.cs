using FishingAssistant.Configuration;
using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class TreasureChancePolicyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Decide_DefaultPreservesVanillaResult(bool vanillaTreasure, bool vanillaGolden)
    {
        TreasureChanceDecision decision = TreasureChancePolicy.Decide(new(
            TreasureChanceBehavior.Default,
            TreasureChanceBehavior.Default,
            vanillaTreasure,
            vanillaGolden,
            IsFestivalFishing: false));

        Assert.Equal(vanillaTreasure, decision.HasTreasure);
        Assert.Equal(vanillaTreasure && vanillaGolden, decision.IsGoldenTreasure);
    }

    [Fact]
    public void Decide_AlwaysCreatesNormalTreasure()
    {
        TreasureChanceDecision decision = TreasureChancePolicy.Decide(new(
            TreasureChanceBehavior.Always,
            TreasureChanceBehavior.Default,
            VanillaTreasure: false,
            VanillaGoldenTreasure: false,
            IsFestivalFishing: false));

        Assert.True(decision.HasTreasure);
        Assert.False(decision.IsGoldenTreasure);
    }

    [Fact]
    public void Decide_GoldenAlwaysCreatesGoldenTreasureWhenTreasureExists()
    {
        TreasureChanceDecision decision = TreasureChancePolicy.Decide(new(
            TreasureChanceBehavior.Always,
            TreasureChanceBehavior.Always,
            VanillaTreasure: false,
            VanillaGoldenTreasure: false,
            IsFestivalFishing: false));

        Assert.True(decision.HasTreasure);
        Assert.True(decision.IsGoldenTreasure);
    }

    [Fact]
    public void Decide_TreasureNeverAlsoDisablesGoldenTreasure()
    {
        TreasureChanceDecision decision = TreasureChancePolicy.Decide(new(
            TreasureChanceBehavior.Never,
            TreasureChanceBehavior.Always,
            VanillaTreasure: true,
            VanillaGoldenTreasure: true,
            IsFestivalFishing: false));

        Assert.False(decision.HasTreasure);
        Assert.False(decision.IsGoldenTreasure);
    }

    [Fact]
    public void Decide_GoldenNeverKeepsNormalTreasure()
    {
        TreasureChanceDecision decision = TreasureChancePolicy.Decide(new(
            TreasureChanceBehavior.Default,
            TreasureChanceBehavior.Never,
            VanillaTreasure: true,
            VanillaGoldenTreasure: true,
            IsFestivalFishing: false));

        Assert.True(decision.HasTreasure);
        Assert.False(decision.IsGoldenTreasure);
    }

    [Fact]
    public void Decide_FestivalFishingPreservesVanillaRules()
    {
        TreasureChanceDecision decision = TreasureChancePolicy.Decide(new(
            TreasureChanceBehavior.Always,
            TreasureChanceBehavior.Always,
            VanillaTreasure: false,
            VanillaGoldenTreasure: false,
            IsFestivalFishing: true));

        Assert.False(decision.HasTreasure);
        Assert.False(decision.IsGoldenTreasure);
    }
}
