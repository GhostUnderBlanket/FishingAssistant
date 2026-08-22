using FishingAssistant.Configuration;
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
    float FlightMilliseconds,
    SteeringEffort Effort);

internal static class BubbleSteeringPolicy
{
    internal const float TickMilliseconds = 1000f / 60f;

    public static bool TryGetTarget(BubbleSteeringConditions conditions, out Vector2 target)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        target = GetTileCenter(conditions.BubbleTile);

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
        float sidewaysSpeed = GetSidewaysSpeed(conditions.FacingDirection, conditions.Effort);
        float availableTicks = Math.Max(1f, conditions.FlightMilliseconds / TickMilliseconds);
        float maximumSidewaysDistance = sidewaysSpeed * availableTicks;
        if (landingForwardTile != bubbleForwardTile)
            return false;

        float centerDifference = horizontalCast
            ? Math.Abs(target.Y - conditions.LandingPixel.Y)
            : Math.Abs(target.X - conditions.LandingPixel.X);
        if (centerDifference <= maximumSidewaysDistance)
            return true;

        target = GetClosestPointInTile(conditions.LandingPixel, conditions.BubbleTile);
        float edgeDifference = horizontalCast
            ? Math.Abs(target.Y - conditions.LandingPixel.Y)
            : Math.Abs(target.X - conditions.LandingPixel.X);
        return edgeDifference <= maximumSidewaysDistance;
    }

    internal static Vector2 GetTileCenter(Point tile)
    {
        return new Vector2(
            (tile.X + 0.5f) * Game1.tileSize,
            (tile.Y + 0.5f) * Game1.tileSize);
    }

    internal static Vector2 GetClosestPointInTile(Vector2 point, Point tile)
    {
        float left = tile.X * Game1.tileSize;
        float top = tile.Y * Game1.tileSize;
        float right = left + Game1.tileSize - 1f;
        float bottom = top + Game1.tileSize - 1f;
        return new Vector2(
            Math.Clamp(point.X, left, right),
            Math.Clamp(point.Y, top, bottom));
    }

    public static Vector2 GetSteeringStep(
        Vector2 current,
        Vector2 target,
        int facingDirection,
        SteeringEffort effort)
    {
        bool horizontalCast = facingDirection is Game1.left or Game1.right;
        float speed = GetSidewaysSpeed(facingDirection, effort);
        return horizontalCast
            ? new Vector2(0f, Math.Clamp(target.Y - current.Y, -speed, speed))
            : new Vector2(Math.Clamp(target.X - current.X, -speed, speed), 0f);
    }

    internal static float GetSidewaysSpeed(int facingDirection, SteeringEffort effort)
    {
        float normalSpeed = facingDirection is Game1.left or Game1.right ? 4f : 2f;
        return normalSpeed * (effort switch
        {
            SteeringEffort.Low => 0.5f,
            SteeringEffort.High => 2f,
            _ => 1f
        });
    }
}
