using FishingAssistant.Runtime;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace FishingAssistant.Fishing;

internal sealed class FishingRodAdapter(Farmer player, FishingRod rod)
{
    public static FishingRodAdapter? ForCurrentPlayer()
    {
        return Context.IsWorldReady && Game1.player.CurrentTool is FishingRod rod
            ? new FishingRodAdapter(Game1.player, rod)
            : null;
    }

    public bool IsTimingCast => rod.isTimingCast;

    public AutoCastConditions ReadAutoCastConditions(
        bool automationEnabled,
        bool autoCastEnabled,
        AutomationState state,
        int castPower)
    {
        float staminaCost = Math.Max(0f, 8f - player.FishingLevel * 0.1f);
        return new AutoCastConditions(
            automationEnabled,
            autoCastEnabled,
            state,
            Context.IsPlayerFree,
            player.CanMove,
            player.isMoving(),
            player.Stamina > staminaCost,
            Game1.isFestival(),
            this.IsCastTargetFishable(castPower)
        );
    }

    public void BeginAutomaticCast(int castPower)
    {
        player.BeginUsingTool();
        this.SetCastPower(castPower);
    }

    public void SetCastPower(int castPower)
    {
        if (rod.isTimingCast)
            rod.castingPower = Math.Clamp(castPower / 100f, 0f, 1f);
    }

    private bool IsCastTargetFishable(int castPower)
    {
        if (!player.currentLocation.canFishHere())
            return false;

        Point target = CalculateTargetTile(
            player.StandingPixel,
            player.FacingDirection,
            player.FishingLevel,
            castPower / 100f
        );
        if (player.currentLocation.isTileFishable(target.X, target.Y))
            return true;

        return player.FacingDirection is Game1.left or Game1.right
            ? player.currentLocation.isTileFishable(target.X, target.Y - 1)
              || player.currentLocation.isTileFishable(target.X, target.Y + 1)
            : player.currentLocation.isTileFishable(target.X - 1, target.Y)
              || player.currentLocation.isTileFishable(target.X + 1, target.Y);
    }

    internal static Point CalculateTargetTile(Point standingPixel, int facingDirection, int fishingLevel,
        float castPower)
    {
        int addedDistance = fishingLevel switch
        {
            >= 15 => 4,
            >= 8 => 3,
            >= 4 => 2,
            >= 1 => 1,
            _ => 0
        };
        float power = Math.Clamp(castPower, 0f, 1f);
        bool horizontal = facingDirection is Game1.left or Game1.right;
        float distance = Math.Max(128f, power * (addedDistance + (horizontal ? 4 : 3)) * Game1.tileSize);
        if (horizontal)
            distance -= 8f;

        Point pixel = facingDirection switch
        {
            Game1.up => new Point(standingPixel.X, standingPixel.Y - (int)distance),
            Game1.right => new Point(standingPixel.X + (int)distance, standingPixel.Y),
            Game1.down => new Point(standingPixel.X, standingPixel.Y + (int)distance),
            Game1.left => new Point(standingPixel.X - (int)distance, standingPixel.Y),
            _ => standingPixel
        };
        return new Point(pixel.X / Game1.tileSize, pixel.Y / Game1.tileSize);
    }
}
