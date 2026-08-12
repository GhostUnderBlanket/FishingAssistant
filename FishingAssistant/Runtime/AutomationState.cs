namespace FishingAssistant.Runtime;

internal enum AutomationState
{
    Idle,
    Ready,
    Casting,
    WaitingForBite,
    Hooking,
    Minigame,
    CatchResult,
    TreasureMenu,
    Cooldown,
    Paused,
    Faulted
}

internal enum AutomationTransitionReason
{
    Observation,
    Enabled,
    Disabled,
    ToolChanged,
    Warped,
    DayStarted,
    SaveLoaded,
    ReturnedToTitle,
    Recovered
}

internal sealed record AutomationTransition(
    AutomationState Previous,
    AutomationState Current,
    AutomationTransitionReason Reason,
    bool WasRecovery);
