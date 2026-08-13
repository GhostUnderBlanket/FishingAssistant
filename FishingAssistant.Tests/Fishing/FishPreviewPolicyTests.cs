using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class FishPreviewPolicyTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Decide_HidesDisabledOrUnreadyPreview(bool enabled, bool ready)
    {
        FishPreviewDecision decision = FishPreviewPolicy.Decide(Conditions(enabled: enabled, ready: ready));

        Assert.False(decision.ShouldDraw);
    }

    [Fact]
    public void Decide_RevealsPreviouslyCaughtFish()
    {
        FishPreviewDecision decision = FishPreviewPolicy.Decide(Conditions(wasCaught: true));

        Assert.True(decision.ShouldDraw);
        Assert.True(decision.RevealFish);
        Assert.True(decision.ShowFishName);
    }

    [Fact]
    public void Decide_HidesUncaughtFishByDefault()
    {
        FishPreviewDecision decision = FishPreviewPolicy.Decide(Conditions());

        Assert.True(decision.ShouldDraw);
        Assert.False(decision.RevealFish);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    public void Decide_UsesExplicitRevealRules(
        bool revealUncaught,
        bool revealLegendary,
        bool legendary)
    {
        FishPreviewDecision decision = FishPreviewPolicy.Decide(Conditions(
            revealUncaught: revealUncaught,
            revealLegendary: revealLegendary,
            legendary: legendary));

        Assert.Equal(revealUncaught || (revealLegendary && legendary), decision.RevealFish);
    }

    [Fact]
    public void Decide_ShowsOnlyAvailableTreasureAndPreservesGoldenStatus()
    {
        FishPreviewDecision available = FishPreviewPolicy.Decide(Conditions(
            showTreasure: true,
            hasTreasure: true,
            goldenTreasure: true));
        FishPreviewDecision unavailable = FishPreviewPolicy.Decide(Conditions(
            showTreasure: true,
            hasTreasure: false,
            goldenTreasure: true));

        Assert.True(available.ShowTreasure);
        Assert.True(available.IsGoldenTreasure);
        Assert.False(unavailable.ShowTreasure);
        Assert.False(unavailable.IsGoldenTreasure);
    }

    private static FishPreviewConditions Conditions(
        bool enabled = true,
        bool ready = true,
        bool wasCaught = false,
        bool legendary = false,
        bool revealUncaught = false,
        bool revealLegendary = false,
        bool showTreasure = true,
        bool hasTreasure = false,
        bool goldenTreasure = false)
    {
        return new FishPreviewConditions(
            enabled,
            ready,
            wasCaught,
            legendary,
            revealUncaught,
            revealLegendary,
            ShowFishName: true,
            showTreasure,
            hasTreasure,
            goldenTreasure);
    }
}
