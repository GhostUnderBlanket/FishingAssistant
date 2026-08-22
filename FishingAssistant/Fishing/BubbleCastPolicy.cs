using FishingAssistant.Configuration;
using Microsoft.Xna.Framework;
using StardewValley;

namespace FishingAssistant.Fishing;

internal sealed record BubbleCastPlanningConditions(
    bool CanFishHere,
    bool IsBubbleTileFishable,
    Point StandingPixel,
    int FacingDirection,
    int FishingLevel,
    Point BubbleTile,
    int RequestedCastPower,
    bool AdjustCastPower,
    SteeringEffort Effort);

internal sealed record BubbleCastPlan(Point BubbleTile, int CastPower, bool IsReachable);

internal static class BubbleCastPolicy
{
    private const float Gravity = 0.005f;

    public static BubbleCastPlan Plan(BubbleCastPlanningConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        int requestedPower = Math.Clamp(conditions.RequestedCastPower, 0, 100);
        if (!conditions.CanFishHere
            || !conditions.IsBubbleTileFishable
            || conditions.BubbleTile == Point.Zero
            || conditions.FacingDirection is < Game1.up or > Game1.left)
        {
            return new BubbleCastPlan(conditions.BubbleTile, requestedPower, false);
        }

        if (!conditions.AdjustCastPower)
        {
            return new BubbleCastPlan(
                conditions.BubbleTile,
                requestedPower,
                IsReachableAtPower(conditions, requestedPower));
        }

        int? selectedPower = null;
        float selectedCenterDistance = float.MaxValue;
        int selectedRequestedDistance = int.MaxValue;
        for (int power = 0; power <= 100; power++)
        {
            if (!IsReachableAtPower(conditions, power))
                continue;

            float centerDistance = GetForwardDistanceToBubbleCenter(conditions, power);
            int requestedDistance = Math.Abs(power - requestedPower);
            if (centerDistance < selectedCenterDistance
                || Math.Abs(centerDistance - selectedCenterDistance) < 0.001f
                && (requestedDistance < selectedRequestedDistance
                    || requestedDistance == selectedRequestedDistance && power < selectedPower))
            {
                selectedPower = power;
                selectedCenterDistance = centerDistance;
                selectedRequestedDistance = requestedDistance;
            }
        }

        return selectedPower is int adjustedPower
            ? new BubbleCastPlan(conditions.BubbleTile, adjustedPower, true)
            : new BubbleCastPlan(conditions.BubbleTile, requestedPower, false);
    }

    public static bool IsReachableAtPower(BubbleCastPlanningConditions conditions, int castPower)
    {
        PredictGeometry(
            conditions.FacingDirection,
            conditions.FishingLevel,
            Math.Clamp(castPower, 0, 100) / 100f,
            out float reach,
            out float flightMilliseconds);

        Vector2 landing = GetLandingPixel(conditions.StandingPixel, conditions.FacingDirection, reach);
        bool horizontal = conditions.FacingDirection is Game1.left or Game1.right;
        int landingForwardTile = horizontal
            ? (int)Math.Floor(landing.X / Game1.tileSize)
            : (int)Math.Floor(landing.Y / Game1.tileSize);
        int bubbleForwardTile = horizontal ? conditions.BubbleTile.X : conditions.BubbleTile.Y;
        if (landingForwardTile != bubbleForwardTile)
            return false;

        Vector2 target = BubbleSteeringPolicy.GetClosestPointInTile(landing, conditions.BubbleTile);
        float sidewaysDifference = horizontal
            ? Math.Abs(target.Y - landing.Y)
            : Math.Abs(target.X - landing.X);
        float sidewaysSpeed = BubbleSteeringPolicy.GetSidewaysSpeed(
            conditions.FacingDirection,
            conditions.Effort);
        float availableTicks = Math.Max(1f, flightMilliseconds / BubbleSteeringPolicy.TickMilliseconds);
        return sidewaysDifference <= sidewaysSpeed * availableTicks;
    }

    public static void PredictGeometry(
        int facingDirection,
        int fishingLevel,
        float castingPower,
        out float reach,
        out float flightMilliseconds)
    {
        int addedDistance = fishingLevel switch
        {
            >= 15 => 4,
            >= 8 => 3,
            >= 4 => 2,
            >= 1 => 1,
            _ => 0
        };
        float power = Math.Clamp(castingPower, 0f, 1f);
        if (facingDirection is Game1.left or Game1.right)
        {
            reach = Math.Max(128f, power * (addedDistance + 4) * Game1.tileSize) - 8f;
            float launchSpeed = reach * MathF.Sqrt(Gravity / (2f * (reach + 96f)));
            flightMilliseconds = 2f * launchSpeed / Gravity
                + (MathF.Sqrt(launchSpeed * launchSpeed + 2f * Gravity * 96f) - launchSpeed) / Gravity;
            return;
        }

        reach = Math.Max(128f, power * (addedDistance + 3) * Game1.tileSize);
        float offset = -reach;
        float arcHeight = Math.Abs(offset - Game1.tileSize);
        if (facingDirection == Game1.up)
        {
            offset = -offset;
            arcHeight += Game1.tileSize;
        }

        float verticalSpeed = MathF.Sqrt(2f * Gravity * arcHeight);
        flightMilliseconds = (MathF.Sqrt(2f * (arcHeight - offset) / Gravity)
                              + verticalSpeed / Gravity) * 1.05f;
        if (facingDirection == Game1.up)
            flightMilliseconds *= 1.05f;
    }

    private static float GetForwardDistanceToBubbleCenter(
        BubbleCastPlanningConditions conditions,
        int castPower)
    {
        PredictGeometry(
            conditions.FacingDirection,
            conditions.FishingLevel,
            Math.Clamp(castPower, 0, 100) / 100f,
            out float reach,
            out _);

        Vector2 landing = GetLandingPixel(conditions.StandingPixel, conditions.FacingDirection, reach);
        Vector2 center = BubbleSteeringPolicy.GetTileCenter(conditions.BubbleTile);
        bool horizontal = conditions.FacingDirection is Game1.left or Game1.right;
        return horizontal ? Math.Abs(landing.X - center.X) : Math.Abs(landing.Y - center.Y);
    }

    private static Vector2 GetLandingPixel(Point standingPixel, int facingDirection, float reach)
    {
        return facingDirection switch
        {
            Game1.up => new Vector2(standingPixel.X, standingPixel.Y - reach),
            Game1.right => new Vector2(standingPixel.X + reach, standingPixel.Y),
            Game1.down => new Vector2(standingPixel.X, standingPixel.Y + reach),
            Game1.left => new Vector2(standingPixel.X - reach, standingPixel.Y),
            _ => standingPixel.ToVector2()
        };
    }
}
