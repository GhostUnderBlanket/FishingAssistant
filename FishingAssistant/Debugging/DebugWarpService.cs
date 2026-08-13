using StardewModdingAPI;
using StardewValley;

namespace FishingAssistant.Debugging;

internal sealed class DebugWarpService(IMonitor monitor, Func<string, string> translate)
{
    internal const string BeachLocationName = "Beach";
    internal const int BeachTileX = 30;
    internal const int BeachTileY = 34;
    internal const int BeachFacingDirection = Game1.down;

    public void WarpToBeachFishingSpot()
    {
        if (!DebugWarpPolicy.CanWarp(
                Context.IsWorldReady,
                Game1.eventUp,
                Game1.currentMinigame is not null))
        {
            if (Context.IsWorldReady)
                Game1.addHUDMessage(new HUDMessage(translate("debug.warp_beach.unavailable"), HUDMessage.error_type));

            monitor.Log(
                $"Couldn't warp local screen {Context.ScreenId} to the beach because the world isn't ready " +
                "or an event or minigame is active.",
                LogLevel.Info);
            return;
        }

        Game1.exitActiveMenu();
        Game1.player.swimming.Value = false;
        Game1.player.changeOutOfSwimSuit();
        Game1.warpFarmer(BeachLocationName, BeachTileX, BeachTileY, BeachFacingDirection);

        monitor.Log(
            $"Warped local screen {Context.ScreenId} to the beach fishing spot at " +
            $"({BeachTileX}, {BeachTileY}).",
            LogLevel.Info);
    }
}
