namespace FishingAssistant.Runtime;

internal sealed record OpenInventoryAfterStopConditions(
    bool Enabled,
    AutomationTransitionReason? StopReason,
    bool IsWorldReady,
    bool IsLocalPlayer,
    bool IsMultiplayer,
    bool HasBlockingMenu,
    bool HasMinigame,
    bool IsEvent,
    bool IsFestival);

internal static class OpenInventoryAfterStopPolicy
{
    public static bool ShouldOpen(OpenInventoryAfterStopConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        return conditions.Enabled
            && conditions.StopReason is AutomationTransitionReason.LateNight
                or AutomationTransitionReason.LowEnergy
            && conditions.IsWorldReady
            && conditions.IsLocalPlayer
            && !conditions.IsMultiplayer
            && !conditions.HasBlockingMenu
            && !conditions.HasMinigame
            && !conditions.IsEvent
            && !conditions.IsFestival;
    }
}
