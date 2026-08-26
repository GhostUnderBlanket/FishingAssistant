#if FISHING_ASSISTANT_TEST_BUILD
using FishingAssistant.Fishing;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace FishingAssistant.Debugging;

internal sealed class DebugFishingBubbleService(IMonitor monitor, Func<string, string> translate)
{
    public void Create(int castPower)
    {
        if (!Context.IsWorldReady || Game1.eventUp || Game1.currentMinigame is not null)
        {
            this.ShowFailure("debug.bubble.unavailable");
            return;
        }

        Farmer player = Game1.player;
        GameLocation location = player.currentLocation;
        Point landing = FishingRodAdapter.CalculateTargetTile(
            player.StandingPixel,
            player.FacingDirection,
            player.FishingLevel,
            castPower / 100f);
        Point? target = this.FindTarget(landing, player.FacingDirection, location);
        if (target is null)
        {
            this.ShowFailure("debug.bubble.no_target");
            return;
        }

        Game1.exitActiveMenu();
        location.fishSplashPoint.Value = target.Value;
        Game1.addHUDMessage(new HUDMessage(
            string.Format(translate("debug.bubble.created"), target.Value.X, target.Value.Y),
            HUDMessage.newQuest_type));
        monitor.Log(
            $"Created a test fishing bubble for local screen {Context.ScreenId} at " +
            $"({target.Value.X}, {target.Value.Y}).",
            LogLevel.Info);
    }

    private Point? FindTarget(Point landing, int facingDirection, GameLocation location)
    {
        bool horizontalCast = facingDirection is Game1.left or Game1.right;
        Point[] offsets = horizontalCast
            ? [new(0, 1), new(0, -1), Point.Zero]
            : [new(1, 0), new(-1, 0), Point.Zero];
        return offsets
            .Select(offset => new Point(landing.X + offset.X, landing.Y + offset.Y))
            .Cast<Point?>()
            .FirstOrDefault(candidate => candidate is Point point && location.isTileFishable(point.X, point.Y));
    }

    private void ShowFailure(string key)
    {
        if (Context.IsWorldReady)
            Game1.addHUDMessage(new HUDMessage(translate(key), HUDMessage.error_type));
        monitor.Log($"Couldn't create a test fishing bubble for local screen {Context.ScreenId}.", LogLevel.Info);
    }
}
#endif
