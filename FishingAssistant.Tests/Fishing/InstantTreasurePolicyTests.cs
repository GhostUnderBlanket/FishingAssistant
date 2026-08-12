using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class InstantTreasurePolicyTests
{
    [Fact]
    public void Decide_CapturesFullyVisibleTreasure()
    {
        Assert.Equal(
            InstantTreasureDecision.Capture,
            InstantTreasurePolicy.Decide(Conditions()));
    }

    [Theory]
    [InlineData(false, true, true, false, 1f, false)]
    [InlineData(true, false, true, false, 1f, false)]
    [InlineData(true, true, false, false, 1f, false)]
    [InlineData(true, true, true, true, 1f, false)]
    [InlineData(true, true, true, false, 0.99f, false)]
    [InlineData(true, true, true, false, 1f, true)]
    public void Decide_WaitsWhenCaptureIsDisabledOrUnsafe(
        bool enabled,
        bool active,
        bool available,
        bool caught,
        float scale,
        bool festival)
    {
        InstantTreasureConditions conditions = new(
            enabled,
            active,
            available,
            caught,
            scale,
            festival);

        Assert.Equal(InstantTreasureDecision.Wait, InstantTreasurePolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_AcceptsScaleAboveVanillaMaximumDefensively()
    {
        Assert.Equal(
            InstantTreasureDecision.Capture,
            InstantTreasurePolicy.Decide(Conditions() with { TreasureScale = 1.2f }));
    }

    private static InstantTreasureConditions Conditions()
    {
        return new(
            Enabled: true,
            IsMinigameActive: true,
            TreasureAvailable: true,
            TreasureCaught: false,
            TreasureScale: 1f,
            IsFestivalFishing: false);
    }
}
