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
    Paused
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
    Saving,
    PeerDisconnected,
    MenuInterrupted,
    LateNight,
    LowEnergy,
    TimedOut,
    Recovered
}

internal sealed record AutomationTransition(
    AutomationState Previous,
    AutomationState Current,
    AutomationTransitionReason Reason,
    bool WasRecovery);
