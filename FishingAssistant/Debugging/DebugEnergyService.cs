#if FISHING_ASSISTANT_TEST_BUILD
using StardewModdingAPI;
using StardewValley;

namespace FishingAssistant.Debugging;

internal sealed class DebugEnergyService(IMonitor monitor, Func<string, string> translate)
{
    public void SetLowEnergy()
    {
        this.SetEnergy(1f, "debug.energy.low");
    }

    public void RestoreEnergy()
    {
        Farmer? player = this.GetPlayer();
        if (player is null)
            return;

        Game1.exitActiveMenu();
        player.Stamina = player.MaxStamina;
        this.ShowSuccess("debug.energy.full");
        monitor.Log($"Restored energy for local screen {Context.ScreenId}.", LogLevel.Info);
    }

    private void SetEnergy(float value, string messageKey)
    {
        Farmer? player = this.GetPlayer();
        if (player is null)
            return;

        Game1.exitActiveMenu();
        player.Stamina = Math.Clamp(value, 0f, player.MaxStamina);
        this.ShowSuccess(messageKey);
        monitor.Log(
            $"Set energy to {player.Stamina:0.##} for local screen {Context.ScreenId}.",
            LogLevel.Info);
    }

    private Farmer? GetPlayer()
    {
        if (Context.IsWorldReady && Game1.player is { IsLocalPlayer: true } player)
            return player;

        if (Context.IsWorldReady)
            Game1.addHUDMessage(new HUDMessage(translate("debug.energy.unavailable"), HUDMessage.error_type));
        monitor.Log($"Couldn't adjust energy for local screen {Context.ScreenId}.", LogLevel.Info);
        return null;
    }

    private void ShowSuccess(string key)
    {
        Game1.addHUDMessage(new HUDMessage(translate(key), HUDMessage.newQuest_type));
    }
}
#endif
