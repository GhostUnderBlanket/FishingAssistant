#if FISHING_ASSISTANT_TEST_BUILD
using StardewModdingAPI;
using StardewValley;

namespace FishingAssistant.Debugging;

internal sealed class DebugWarpService(IMonitor monitor, Func<string, string> translate)
{
    private const string BeachLocationName = "Beach";
    private const int BeachTileX = 30;
    private const int BeachTileY = 34;

    public void WarpToBeachFishingSpot()
    {
        if (!Context.IsWorldReady || Game1.eventUp || Game1.currentMinigame is not null)
        {
            if (Context.IsWorldReady)
                Game1.addHUDMessage(new HUDMessage(translate("debug.warp_beach.unavailable"), HUDMessage.error_type));
            monitor.Log($"Couldn't warp local screen {Context.ScreenId} to the beach.", LogLevel.Info);
            return;
        }

        Game1.exitActiveMenu();
        Game1.player.swimming.Value = false;
        Game1.player.changeOutOfSwimSuit();
        Game1.warpFarmer(BeachLocationName, BeachTileX, BeachTileY, Game1.down);
        monitor.Log($"Warped local screen {Context.ScreenId} to the beach fishing spot.", LogLevel.Info);
    }
}
#endif
