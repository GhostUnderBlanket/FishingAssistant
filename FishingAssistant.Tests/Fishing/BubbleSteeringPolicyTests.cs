using FishingAssistant.Fishing;
using Microsoft.Xna.Framework;
using StardewValley;

namespace FishingAssistant.Tests.Fishing;

public sealed class BubbleSteeringPolicyTests
{
    private static BubbleSteeringConditions ReachableVerticalCast => new(
        Enabled: true,
        IsManualCast: true,
        IsBobberInAir: true,
        CanFishHere: true,
        IsBubbleTileFishable: true,
        FacingDirection: Game1.down,
        StandingPixel: new Vector2(320f, 320f),
        LandingPixel: new Vector2(320f, 640f),
        BubbleTile: new Point(6, 10),
        FlightMilliseconds: 800f);

    [Fact]
    public void TryGetTarget_AcceptsReachableManualCast()
    {
        Assert.True(BubbleSteeringPolicy.TryGetTarget(ReachableVerticalCast, out Vector2 target));
        Assert.Equal(new Vector2(416f, 672f), target);
    }

    [Fact]
    public void TryGetTarget_RejectsAutomaticCast()
    {
        Assert.False(BubbleSteeringPolicy.TryGetTarget(
            ReachableVerticalCast with { IsManualCast = false }, out _));
    }

    [Fact]
    public void TryGetTarget_RejectsBubbleOutsideForwardCastDistance()
    {
        Assert.False(BubbleSteeringPolicy.TryGetTarget(
            ReachableVerticalCast with { BubbleTile = new Point(6, 12) }, out _));
    }

    [Fact]
    public void TryGetTarget_RejectsBubbleBeyondVanillaSidewaysDrift()
    {
        Assert.False(BubbleSteeringPolicy.TryGetTarget(
            ReachableVerticalCast with { BubbleTile = new Point(10, 10) }, out _));
    }

    [Theory]
    [InlineData(Game1.down, 2, 0)]
    [InlineData(Game1.up, 2, 0)]
    [InlineData(Game1.left, 0, 4)]
    [InlineData(Game1.right, 0, 4)]
    public void GetSteeringStep_MatchesVanillaSidewaysSpeed(int facing, float x, float y)
    {
        Vector2 step = BubbleSteeringPolicy.GetSteeringStep(
            new Vector2(100f, 100f), new Vector2(200f, 200f), facing);

        Assert.Equal(new Vector2(x, y), step);
    }
}
