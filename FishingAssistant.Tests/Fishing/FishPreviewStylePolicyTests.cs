using FishingAssistant.Configuration;
using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class FishPreviewStylePolicyTests
{
    [Theory]
    [InlineData(false, false, (int)FishPreviewStyle.Classic, false)]
    [InlineData(true, false, (int)FishPreviewStyle.Classic, true)]
    [InlineData(false, true, (int)FishPreviewStyle.Classic, false)]
    [InlineData(false, true, (int)FishPreviewStyle.Sonar, true)]
    [InlineData(true, true, (int)FishPreviewStyle.Sonar, true)]
    public void ShouldReserveChallengeBaitSpace_OnlyAddsSpaceForModSonar(
        bool hasVanillaSonarBobber,
        bool fishPreviewEnabled,
        int previewStyle,
        bool expected)
    {
        Assert.Equal(expected, FishPreviewStylePolicy.ShouldReserveChallengeBaitSpace(
            hasVanillaSonarBobber,
            fishPreviewEnabled,
            (FishPreviewStyle)previewStyle));
    }

    [Theory]
    [InlineData((int)FishPreviewStyle.Classic)]
    [InlineData((int)FishPreviewStyle.Sonar)]
    public void Decide_UsesConfiguredStyleWhenVanillaCanBeSuppressed(int styleValue)
    {
        FishPreviewStyle style = (FishPreviewStyle)styleValue;

        FishPreviewStyleDecision decision = FishPreviewStylePolicy.Decide(new(
            style, CanSuppressVanillaPreview: true, HasEquippedSonarBobber: true));

        Assert.Equal(style, decision.EffectiveStyle);
        Assert.True(decision.ShouldDrawModPreview);
        Assert.False(decision.UsedCompatibilityFallback);
    }

    [Fact]
    public void Decide_LeavesVanillaAloneWhenSuppressionFailsWithSonarEquipped()
    {
        FishPreviewStyleDecision decision = FishPreviewStylePolicy.Decide(new(
            FishPreviewStyle.Sonar,
            CanSuppressVanillaPreview: false,
            HasEquippedSonarBobber: true));

        Assert.False(decision.ShouldDrawModPreview);
        Assert.True(decision.UsedCompatibilityFallback);
    }

    [Fact]
    public void Decide_FallsBackToClassicWhenSuppressionFailsWithoutSonarEquipped()
    {
        FishPreviewStyleDecision decision = FishPreviewStylePolicy.Decide(new(
            FishPreviewStyle.Sonar,
            CanSuppressVanillaPreview: false,
            HasEquippedSonarBobber: false));

        Assert.Equal(FishPreviewStyle.Classic, decision.EffectiveStyle);
        Assert.True(decision.ShouldDrawModPreview);
        Assert.True(decision.UsedCompatibilityFallback);
    }
}
