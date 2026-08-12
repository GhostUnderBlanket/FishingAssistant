using FishingAssistant.Fishing;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace FishingAssistant.Runtime;

internal sealed class AutomationRuntime(IMonitor monitor)
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

    private sealed class ScreenContext
    {
        public AutomationSession Session { get; } = new();

        public Tool? LastTool { get; set; }

        public bool HasObservedTool { get; set; }
    }
}
