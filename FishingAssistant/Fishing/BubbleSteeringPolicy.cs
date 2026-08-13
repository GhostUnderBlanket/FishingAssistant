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

    public static bool TryGetTarget(BubbleSteeringConditions conditions, out Vector2 target)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        target = GetClosestPointInTile(conditions.LandingPixel, conditions.BubbleTile);

        if (!conditions.Enabled
            || !conditions.IsBobberInAir
            || !conditions.CanFishHere
            || !conditions.IsBubbleTileFishable
            || conditions.BubbleTile == Point.Zero
            || conditions.FlightMilliseconds <= 0f)
            return false;

        bool horizontalCast = conditions.FacingDirection is Game1.left or Game1.right;
        int landingForwardTile = horizontalCast
            ? (int)Math.Floor(conditions.LandingPixel.X / Game1.tileSize)
            : (int)Math.Floor(conditions.LandingPixel.Y / Game1.tileSize);
        int bubbleForwardTile = horizontalCast ? conditions.BubbleTile.X : conditions.BubbleTile.Y;
        float sidewaysDifference = horizontalCast
            ? Math.Abs(target.Y - conditions.LandingPixel.Y)
            : Math.Abs(target.X - conditions.LandingPixel.X);
        float sidewaysSpeed = horizontalCast ? 4f : 2f;
        float availableTicks = Math.Max(1f, conditions.FlightMilliseconds / TickMilliseconds);

        return landingForwardTile == bubbleForwardTile
            && sidewaysDifference <= sidewaysSpeed * availableTicks;
    }

    private static Vector2 GetClosestPointInTile(Vector2 point, Point tile)
    {
        float left = tile.X * Game1.tileSize;
        float top = tile.Y * Game1.tileSize;
        float right = left + Game1.tileSize - 1f;
        float bottom = top + Game1.tileSize - 1f;
        return new Vector2(
            Math.Clamp(point.X, left, right),
            Math.Clamp(point.Y, top, bottom));
    }

    public static Vector2 GetSteeringStep(Vector2 current, Vector2 target, int facingDirection)
    {
        bool horizontalCast = facingDirection is Game1.left or Game1.right;
        return horizontalCast
            ? new Vector2(0f, Math.Clamp(target.Y - current.Y, -4f, 4f))
            : new Vector2(Math.Clamp(target.X - current.X, -2f, 2f), 0f);
    }
}
