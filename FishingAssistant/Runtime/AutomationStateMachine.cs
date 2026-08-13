namespace FishingAssistant.Runtime;

internal static class AutomationStateMachine
{
    public static AutomationState Classify(FishingObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        if (!observation.IsEnabled || !observation.IsWorldReady)
            return AutomationState.Idle;
        if (observation.IsTreasureMenu)
            return AutomationState.TreasureMenu;
        if (observation.IsMinigame)
            return AutomationState.Minigame;
        if (!observation.HasFishingRod)
            return AutomationState.Idle;
        if (observation.IsFishCaught || observation.IsPullingOutOfWater)
            return AutomationState.CatchResult;
        if (observation.IsNibbling || observation.IsReeling)
            return AutomationState.Hooking;
        if (observation.IsTimingCast || observation.IsCasting || observation.IsBobberInAir)
            return AutomationState.Casting;
        if (observation.IsFishing)
            return AutomationState.WaitingForBite;
        if (observation.HasBlockingMenu)
            return AutomationState.Paused;
        return AutomationState.Ready;
    }

    public static bool IsLegalTransition(AutomationState previous, AutomationState current)
    {
        if (previous == current)
            return true;
        if (current is AutomationState.Idle or AutomationState.Paused)
            return true;

        return previous switch
        {
            AutomationState.Idle => current == AutomationState.Ready,
            AutomationState.Ready => current == AutomationState.Casting,
            AutomationState.Casting => current == AutomationState.WaitingForBite,
            AutomationState.WaitingForBite => current is AutomationState.Hooking or AutomationState.CatchResult,
            AutomationState.Hooking => current is AutomationState.Minigame
                or AutomationState.CatchResult
                or AutomationState.WaitingForBite,
            AutomationState.Minigame => current is AutomationState.CatchResult or AutomationState.TreasureMenu,
            AutomationState.CatchResult => current is AutomationState.TreasureMenu
                or AutomationState.Ready,
            AutomationState.TreasureMenu => current == AutomationState.Ready,
            AutomationState.Paused => true,
            _ => false
        };
    }
}
