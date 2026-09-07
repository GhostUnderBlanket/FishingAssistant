using FishingAssistant.Configuration;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace FishingAssistant.Runtime;

internal sealed class LateNightService(IMonitor monitor, Func<string, string> translate)
{
    private readonly PerScreen<ScreenState> screens = new(() => new ScreenState());

    public void OnTimeChanged(ModConfig config, AutomationSession session, int newTime)
    {
        ScreenState screen = this.screens.Value;
        this.SynchronizeConfig(screen, config);
        LateNightWarningConditions conditions = new(
            config.AutoPauseFishing,
            session.IsEnabled,
            this.IsFishingContext(),
            newTime,
            config.TimeToPause * 100,
            screen.WarningsIssued,
            config.WarnCount
        );
        LateNightWarningAction action = LateNightPolicy.DecideWarning(conditions);
        if (action == LateNightWarningAction.None)
            return;

        screen.WarningsIssued++;
        if (action == LateNightWarningAction.WarnAndRequestPause)
            screen.PausePending = true;

        string configuredTime = Game1.getTimeOfDayString(config.TimeToPause * 100);
        Game1.addHUDMessage(new HUDMessage(string.Format(
            translate("hud.late_night.warning"),
            configuredTime,
            screen.WarningsIssued,
            Math.Max(1, config.WarnCount)), HUDMessage.error_type));
        monitor.Log(
            $"Issued late-night fishing warning {screen.WarningsIssued}/{Math.Max(1, config.WarnCount)} " +
            $"for local screen {Context.ScreenId} at {newTime}.",
            LogLevel.Info);
    }

    public AutomationTransition? UpdateCurrent(ModConfig config, AutomationSession session)
    {
        ScreenState screen = this.screens.Value;
        this.SynchronizeConfig(screen, config);
        if (!session.IsEnabled)
        {
            screen.PausePending = false;
            return null;
        }

        FishingRod? rod = Context.IsWorldReady ? Game1.player.CurrentTool as FishingRod : null;
        LateNightPauseConditions conditions = new(
            screen.PausePending,
            session.IsEnabled,
            Context.IsWorldReady,
            Context.IsWorldReady && Game1.player.IsLocalPlayer,
            rod?.inUse() == true,
            Game1.activeClickableMenu is not null,
            Game1.currentMinigame is not null,
            Game1.eventUp,
            Game1.isFestival()
        );
        if (!LateNightPolicy.ShouldPause(conditions))
            return null;

        screen.PausePending = false;
        Game1.addHUDMessage(new HUDMessage(translate("hud.late_night.paused"), HUDMessage.error_type));
        monitor.Log(
            $"Paused fishing automation safely after late-night warnings for local screen {Context.ScreenId}.",
            LogLevel.Info);
        return session.Disable(AutomationTransitionReason.LateNight);
    }

    public void ResetCurrent()
    {
        this.screens.Value.Reset();
    }

    public void ResetAll()
    {
        this.screens.ResetAllScreens();
    }

    private bool IsFishingContext()
    {
        return Context.IsWorldReady
            && Game1.player.IsLocalPlayer
            && Game1.player.CurrentTool is FishingRod
            && !Game1.eventUp
            && !Game1.isFestival();
    }

    private void SynchronizeConfig(ScreenState screen, ModConfig config)
    {
        ConfigSignature signature = new(config.AutoPauseFishing, config.TimeToPause, config.WarnCount);
        if (screen.Signature == signature)
            return;

        screen.Reset();
        screen.Signature = signature;
    }

    private sealed class ScreenState
    {
        public ConfigSignature? Signature { get; set; }

        public int WarningsIssued { get; set; }

        public bool PausePending { get; set; }

        public void Reset()
        {
            this.Signature = null;
            this.WarningsIssued = 0;
            this.PausePending = false;
        }
    }

    private sealed record ConfigSignature(
        PauseFishingBehavior Behavior,
        int TimeToPause,
        int WarnCount);
}
