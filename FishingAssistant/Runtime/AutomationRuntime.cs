using FishingAssistant.Configuration;
using FishingAssistant.Fishing;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace FishingAssistant.Runtime;

internal sealed class AutomationRuntime(IMonitor monitor, Func<ModConfig> getConfig)
{
    private readonly PerScreen<ScreenContext> screens = new(() => new ScreenContext());

    public AutomationSession Current => this.screens.Value.Session;

    public void UpdateCurrent()
    {
        if (!Context.IsWorldReady)
            return;

        ScreenContext screen = this.screens.Value;
        Tool? currentTool = Game1.player.CurrentTool;
        if (screen.HasObservedTool && !ReferenceEquals(screen.LastTool, currentTool))
            this.Log(screen.Session.Reset(AutomationTransitionReason.ToolChanged));

        screen.LastTool = currentTool;
        screen.HasObservedTool = true;
        this.Log(screen.Session.Observe(FishingContextReader.Read(screen.Session.IsEnabled)));
        this.UpdateAutomaticHook(screen);
        this.UpdateAutomaticCast(screen);
    }

    public void ToggleCurrent()
    {
        AutomationTransition transition = this.screens.Value.Session.Toggle();
        monitor.Log(
            $"Automation {(this.Current.IsEnabled ? "enabled" : "disabled")} for local screen {Context.ScreenId}.",
            LogLevel.Info);
        this.Log(transition);
    }

    public void ResetCurrent(AutomationTransitionReason reason)
    {
        ScreenContext screen = this.screens.Value;
        screen.HasObservedTool = false;
        screen.LastTool = null;
        screen.ReadyTicks = 0;
        screen.AutomaticCastInProgress = false;
        screen.HookAttemptedForNibble = false;
        this.Log(screen.Session.Reset(reason));
    }

    public void ResetAll(AutomationTransitionReason reason)
    {
        foreach (AutomationSession session in this.screens.GetActiveValues().Select(pair => pair.Value.Session))
            session.Reset(reason);
        this.screens.ResetAllScreens();
    }

    private void Log(AutomationTransition? transition)
    {
        if (transition is null)
            return;

        monitor.Log(
            $"Automation state for local screen {Context.ScreenId}: {transition.Previous} -> " +
            $"{transition.Current} ({transition.Reason}{(transition.WasRecovery ? ", recovered" : "")}).",
            transition.WasRecovery ? LogLevel.Debug : LogLevel.Trace);
    }

    private void UpdateAutomaticCast(ScreenContext screen)
    {
        ModConfig config = getConfig();
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
        {
            screen.ReadyTicks = 0;
            screen.AutomaticCastInProgress = false;
            return;
        }

        if (screen.AutomaticCastInProgress)
        {
            if (rod.IsTimingCast)
                rod.SetCastPower(config.DefaultCastPower);

            if (screen.Session.State is AutomationState.Ready or AutomationState.Casting)
            {
                screen.ReadyTicks = 0;
                return;
            }

            screen.AutomaticCastInProgress = false;
        }

        AutoCastConditions conditions = rod.ReadAutoCastConditions(
            screen.Session.IsEnabled,
            config.AutoCastFishingRod,
            screen.Session.State,
            config.DefaultCastPower
        );
        int requiredTicks = (int)Math.Ceiling(config.AutoCastDelaySeconds * 60f);
        if (rod.IsSupportedFishingMinigame)
            requiredTicks = Math.Max(requiredTicks, 75);
        switch (AutoCastPolicy.Decide(conditions, screen.ReadyTicks, requiredTicks))
        {
            case AutoCastDecision.Reset:
                screen.ReadyTicks = 0;
                break;
            case AutoCastDecision.Wait:
                screen.ReadyTicks++;
                break;
            case AutoCastDecision.Cast:
                screen.ReadyTicks = 0;
                screen.AutomaticCastInProgress = true;
                rod.BeginAutomaticCast(config.DefaultCastPower);
                monitor.Log($"Started an automatic cast for local screen {Context.ScreenId}.", LogLevel.Trace);
                break;
        }
    }

    private void UpdateAutomaticHook(ScreenContext screen)
    {
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
        {
            screen.HookAttemptedForNibble = false;
            return;
        }

        ModConfig config = getConfig();
        AutoHookConditions conditions = rod.ReadAutoHookConditions(
            screen.Session.IsEnabled,
            config.AutoHookFish,
            screen.Session.State,
            screen.HookAttemptedForNibble
        );
        switch (AutoHookPolicy.Decide(conditions))
        {
            case AutoHookDecision.ResetAttempt:
                screen.HookAttemptedForNibble = false;
                break;
            case AutoHookDecision.Wait:
                break;
            case AutoHookDecision.Hook:
                screen.HookAttemptedForNibble = true;
                rod.HookFish();
                monitor.Log($"Hooked a fish automatically for local screen {Context.ScreenId}.", LogLevel.Trace);
                break;
        }
    }

    private sealed class ScreenContext
    {
        public AutomationSession Session { get; } = new();

        public Tool? LastTool { get; set; }

        public bool HasObservedTool { get; set; }

        public int ReadyTicks { get; set; }

        public bool AutomaticCastInProgress { get; set; }

        public bool HookAttemptedForNibble { get; set; }
    }
}
