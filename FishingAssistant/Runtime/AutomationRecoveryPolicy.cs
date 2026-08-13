namespace FishingAssistant.Runtime;

internal enum PendingAutomationAction
{
    None,
    AutomaticCast,
    AutomaticHook,
    CatchPopupClose
}

internal sealed record AutomationRecoveryConditions(
    AutomationState State,
    bool AutomaticCastInProgress,
    bool HookAttempted,
    bool CatchPopupCloseAttempted);

internal static class AutomationRecoveryPolicy
{
    public const int AutomaticCastTimeoutTicks = 10 * 60;
    public const int AutomaticHookTimeoutTicks = 5 * 60;
    public const int CatchPopupCloseTimeoutTicks = 5 * 60;

    public static PendingAutomationAction GetPendingAction(AutomationRecoveryConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (conditions.AutomaticCastInProgress
            && conditions.State is AutomationState.Ready or AutomationState.Casting)
        {
            return PendingAutomationAction.AutomaticCast;
        }
        if (conditions.HookAttempted && conditions.State == AutomationState.Hooking)
            return PendingAutomationAction.AutomaticHook;
        if (conditions.CatchPopupCloseAttempted && conditions.State == AutomationState.CatchResult)
            return PendingAutomationAction.CatchPopupClose;
        return PendingAutomationAction.None;
    }

    public static bool ShouldCancelForBlockingMenu(
        AutomationRecoveryConditions conditions,
        bool hasBlockingMenu)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        return hasBlockingMenu
            && (conditions.AutomaticCastInProgress
                || conditions.HookAttempted
                || conditions.CatchPopupCloseAttempted);
    }

    public static bool HasTimedOut(PendingAutomationAction action, int elapsedTicks)
    {
        int timeout = action switch
        {
            PendingAutomationAction.AutomaticCast => AutomaticCastTimeoutTicks,
            PendingAutomationAction.AutomaticHook => AutomaticHookTimeoutTicks,
            PendingAutomationAction.CatchPopupClose => CatchPopupCloseTimeoutTicks,
            _ => int.MaxValue
        };
        return elapsedTicks >= timeout;
    }
}
