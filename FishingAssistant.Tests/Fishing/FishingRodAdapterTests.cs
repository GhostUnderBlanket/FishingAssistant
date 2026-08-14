using FishingAssistant.Fishing;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Tools;

namespace FishingAssistant.Tests.Fishing;

public sealed class FishingRodAdapterTests
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void IsCastInProgressFor_DetectsEveryCastingPhase(
        bool isTimingCast,
        bool isCasting,
        bool bobberInAir)
    {
        FishingRod rod = new()
        {
            isTimingCast = isTimingCast,
            isCasting = isCasting,
            castedButBobberStillInAir = bobberInAir
        };

        Assert.True(FishingRodAdapter.IsCastInProgressFor(rod));
    }

    [Fact]
    public void IsCastInProgressFor_ReturnsFalseAfterCastCompletes()
    {
        Assert.False(FishingRodAdapter.IsCastInProgressFor(new FishingRod()));
    }

    [Fact]
    public void ResetCancelledCastState_ClearsEveryCastingPhaseIncludingBobberFlight()
    {
        StardewValley.Tools.FishingRod rod = new()
        {
            isTimingCast = true,
            isCasting = true,
            castedButBobberStillInAir = true
        };

        FishingRodAdapter.ResetCancelledCastState(rod);

        Assert.False(rod.isTimingCast);
        Assert.False(rod.isCasting);
        Assert.False(rod.castedButBobberStillInAir);
    }

    [Theory]
    [InlineData(Game1.up, 10, 7)]
    [InlineData(Game1.right, 13, 10)]
    [InlineData(Game1.down, 10, 13)]
    [InlineData(Game1.left, 6, 10)]
    public void CalculateTargetTile_ProjectsInFacingDirection(int direction, int expectedX, int expectedY)
    {
        Point result = FishingRodAdapter.CalculateTargetTile(new Point(640, 640), direction, 0, 1f);

        Assert.Equal(new Point(expectedX, expectedY), result);
    }

    [Fact]
    public void CalculateTargetTile_UsesFishingLevelForCastDistance()
    {
        Point lowLevel = FishingRodAdapter.CalculateTargetTile(new Point(640, 640), Game1.down, 0, 1f);
        Point highLevel = FishingRodAdapter.CalculateTargetTile(new Point(640, 640), Game1.down, 15, 1f);

        Assert.Equal(new Point(10, 13), lowLevel);
        Assert.Equal(new Point(10, 17), highLevel);
    }
}
