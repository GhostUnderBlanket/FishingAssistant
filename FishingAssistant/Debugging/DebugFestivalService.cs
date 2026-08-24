#if FISHING_ASSISTANT_TEST_BUILD
using StardewModdingAPI;
using StardewValley;

namespace FishingAssistant.Debugging;

internal sealed class DebugFestivalService(IMonitor monitor, Func<string, string> translate)
{
    public void PrepareIceFishingFestival()
    {
        this.PrepareFestival("winter8", "Ice Fishing Festival");
    }

    public void PrepareStardewValleyFair()
    {
        this.PrepareFestival("fall16", "Stardew Valley Fair");
    }

    private void PrepareFestival(string festivalId, string festivalName)
    {
        if (!Context.IsWorldReady
            || !Context.IsMainPlayer
            || Game1.eventUp
            || Game1.currentMinigame is not null)
        {
            if (Context.IsWorldReady)
                Game1.addHUDMessage(new HUDMessage(translate("debug.festival.unavailable"), HUDMessage.error_type));
            monitor.Log($"Couldn't prepare {festivalName} for local screen {Context.ScreenId}.", LogLevel.Info);
            return;
        }

        Game1.exitActiveMenu();
        Game1.game1.parseDebugInput($"Festival {festivalId}");
        Game1.addHUDMessage(new HUDMessage(translate("debug.festival.prepared"), HUDMessage.newQuest_type));
        monitor.Log(
            $"Prepared {festivalName} for testing from local screen {Context.ScreenId}.",
            LogLevel.Warn);
    }
}
#endif
