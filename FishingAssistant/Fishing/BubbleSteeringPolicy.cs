using Microsoft.Xna.Framework;
using StardewValley;

namespace FishingAssistant.Fishing;

internal sealed record BubbleSteeringConditions(
    bool Enabled,
    bool IsBobberInAir,
    bool CanFishHere,
    bool IsBubbleTileFishable,
    int FacingDirection,
    Vector2 StandingPixel,
    Vector2 LandingPixel,
    Point BubbleTile,
    float FlightMilliseconds);

internal static class BubbleSteeringPolicy
{
    internal const float TickMilliseconds = 1000f / 60f;
    internal const float ForwardTolerance = Game1.tileSize / 2f;

    public static bool TryGetTarget(BubbleSteeringConditions conditions, out Vector2 target)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        target = new Vector2(
            conditions.BubbleTile.X * Game1.tileSize + Game1.tileSize / 2f,
            conditions.BubbleTile.Y * Game1.tileSize + Game1.tileSize / 2f);

        if (!conditions.Enabled
            || !conditions.IsBobberInAir
            || !conditions.CanFishHere
            || !conditions.IsBubbleTileFishable
            || conditions.BubbleTile == Point.Zero
            || conditions.FlightMilliseconds <= 0f)
            return false;

        bool horizontalCast = conditions.FacingDirection is Game1.left or Game1.right;
        float forwardDifference = horizontalCast
            ? Math.Abs(target.X - conditions.LandingPixel.X)
            : Math.Abs(target.Y - conditions.LandingPixel.Y);
        float sidewaysDifference = horizontalCast
            ? Math.Abs(target.Y - conditions.LandingPixel.Y)
            : Math.Abs(target.X - conditions.LandingPixel.X);
        float sidewaysSpeed = horizontalCast ? 4f : 2f;
        float availableTicks = Math.Max(1f, conditions.FlightMilliseconds / TickMilliseconds);

        return forwardDifference <= ForwardTolerance
            && sidewaysDifference <= sidewaysSpeed * availableTicks;
    }

    public static Vector2 GetSteeringStep(Vector2 current, Vector2 target, int facingDirection)
    {
        bool horizontalCast = facingDirection is Game1.left or Game1.right;
        return horizontalCast
            ? new Vector2(0f, Math.Clamp(target.Y - current.Y, -4f, 4f))
            : new Vector2(Math.Clamp(target.X - current.X, -2f, 2f), 0f);
    }
}
