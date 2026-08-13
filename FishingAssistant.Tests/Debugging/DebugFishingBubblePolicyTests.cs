using FishingAssistant.Debugging;
using Microsoft.Xna.Framework;
using StardewValley;

namespace FishingAssistant.Tests.Debugging;

public sealed class DebugFishingBubblePolicyTests
{
    [Fact]
    public void FindTarget_PrefersSidewaysTileForVerticalCast()
    {
        Point? result = DebugFishingBubblePolicy.FindTarget(
            new Point(10, 12), Game1.down, (x, y) => x == 11 && y == 12);

        Assert.Equal(new Point(11, 12), result);
    }

    [Fact]
    public void FindTarget_PrefersSidewaysTileForHorizontalCast()
    {
        Point? result = DebugFishingBubblePolicy.FindTarget(
            new Point(12, 10), Game1.right, (x, y) => x == 12 && y == 11);

        Assert.Equal(new Point(12, 11), result);
    }

    [Fact]
    public void FindTarget_FallsBackToDirectLandingTile()
    {
        Point? result = DebugFishingBubblePolicy.FindTarget(
            new Point(10, 12), Game1.down, (x, y) => x == 10 && y == 12);

        Assert.Equal(new Point(10, 12), result);
    }

    [Fact]
    public void FindTarget_ReturnsNullWhenNoCandidateIsFishable()
    {
        Assert.Null(DebugFishingBubblePolicy.FindTarget(
            new Point(10, 12), Game1.down, (_, _) => false));
    }
}
