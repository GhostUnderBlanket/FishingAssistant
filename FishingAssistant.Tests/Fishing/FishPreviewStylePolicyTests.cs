using FishingAssistant.Configuration;
using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class FishPreviewStylePolicyTests
{
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
