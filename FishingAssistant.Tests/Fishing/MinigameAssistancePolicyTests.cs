using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class MinigameAssistancePolicyTests
{
    [Theory]
    [InlineData(4.5f)]
    [InlineData(-2.25f)]
    [InlineData(0f)]
    public void FishSpeed_OneHundredPercentIsIdentity(float vanilla)
    {
        Assert.Equal(vanilla, MinigameAssistancePolicy.ScaleFishMovement(vanilla, 100));
    }

    [Fact]
    public void FishSpeed_ScalesFinalVanillaMovement()
    {
        Assert.Equal(2f, MinigameAssistancePolicy.ScaleFishMovement(4f, 50));
    }

    [Fact]
    public void ProgressGain_OneHundredPercentIsIdentity()
    {
        Assert.Equal(0.002f, MinigameAssistancePolicy.ScaleProgressGain(0.002f, 100));
    }

    [Fact]
    public void ProgressGain_ScalesVanillaGain()
    {
        Assert.Equal(0.0035f, MinigameAssistancePolicy.ScaleProgressGain(0.002f, 175), 6);
    }

    [Fact]
    public void ProgressLoss_OneHundredPercentIsIdentity()
    {
        Assert.Equal(0.5f, MinigameAssistancePolicy.ScaleProgressLossModifier(0.5f, 100));
    }

    [Fact]
    public void ProgressLoss_ScalesVanillaPenaltyModifier()
    {
        Assert.Equal(0.3f, MinigameAssistancePolicy.ScaleProgressLossModifier(0.5f, 60), 6);
    }

    [Fact]
    public void ProgressLoss_ZeroPercentPreventsLoss()
    {
        Assert.Equal(0f, MinigameAssistancePolicy.ScaleProgressLossModifier(1f, 0));
    }

    [Fact]
    public void TreasureSpeed_OneHundredPercentIsIdentity()
    {
        Assert.Equal(0.0135f, MinigameAssistancePolicy.ScaleTreasureGain(0.0135f, 100));
    }

    [Fact]
    public void TreasureSpeed_ScalesVanillaGain()
    {
        Assert.Equal(0.030375f, MinigameAssistancePolicy.ScaleTreasureGain(0.0135f, 225), 6);
    }

    [Fact]
    public void BarSize_OneHundredPercentIsIdentity()
    {
        Assert.Equal(176, MinigameAssistancePolicy.ScaleBarSize(176, 100));
    }

    [Fact]
    public void BarSize_ScalesVanillaCalculatedHeight()
    {
        Assert.Equal(220, MinigameAssistancePolicy.ScaleBarSize(176, 125));
    }

    [Theory]
    [InlineData(500, 200, 568)]
    [InlineData(1, 50, 16)]
    public void BarSize_ClampsToSafeTrackDimensions(int vanilla, int percent, int expected)
    {
        Assert.Equal(expected, MinigameAssistancePolicy.ScaleBarSize(vanilla, percent));
    }
}
