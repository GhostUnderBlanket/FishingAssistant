using StardewModdingAPI;
using StardewValley;

namespace FishingAssistant.Debugging;

internal sealed class DebugFestivalService(IMonitor monitor, Func<string, string> translate)
{
    internal const string IceFishingFestivalId = "winter8";
    internal const string StardewValleyFairFestivalId = "fall16";

    public void PrepareIceFishingFestival()
    {
        this.PrepareFestival(IceFishingFestivalId, "Ice Fishing Festival");
    }

    public void PrepareStardewValleyFair()
    {
        this.PrepareFestival(StardewValleyFairFestivalId, "Stardew Valley Fair");
    }

    private void PrepareFestival(string festivalId, string festivalName)
    {
        if (!DebugFestivalPolicy.CanPrepareFestival(
                Context.IsWorldReady,
                Context.IsMainPlayer,
                Game1.eventUp,
                Game1.currentMinigame is not null))
        {
            if (Context.IsWorldReady)
                Game1.addHUDMessage(new HUDMessage(translate("debug.festival.unavailable"), HUDMessage.error_type));

            monitor.Log(
                $"Couldn't prepare {festivalName} for local screen {Context.ScreenId} because " +
                "the current player isn't the host or an event or minigame is active.",
                LogLevel.Info);
            return;
        }

        Game1.exitActiveMenu();
        Game1.game1.parseDebugInput($"Festival {festivalId}");
        Game1.addHUDMessage(new HUDMessage(translate("debug.festival.prepared"), HUDMessage.newQuest_type));
        monitor.Log(
            $"Prepared {festivalName} for testing from local screen {Context.ScreenId}. " +
            "The Vanilla debug command changed the current season, day, time, and location.",
            LogLevel.Warn);
    }
}
