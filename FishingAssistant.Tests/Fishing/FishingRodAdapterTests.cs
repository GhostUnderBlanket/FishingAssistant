using FishingAssistant.Fishing;
using Microsoft.Xna.Framework;
using StardewValley;

namespace FishingAssistant.Tests.Fishing;

public sealed class FishingRodAdapterTests
{
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
