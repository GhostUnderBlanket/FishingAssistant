using FishingAssistant.Runtime;

namespace FishingAssistant.Fishing;

internal enum AutoClosePopupDecision
{
    Reset,
    Wait,
    Close
}

internal sealed record AutoClosePopupConditions(
    bool AutomationEnabled,
    bool AutoClosePopupEnabled,
    AutomationState State,
    bool IsFishPopupVisible,
    bool HasBlockingMenu,
    bool IsFestival,
    bool CloseAlreadyAttempted)
{
    public bool IsEligible => AutomationEnabled
        && AutoClosePopupEnabled
        && State == AutomationState.CatchResult
        && IsFishPopupVisible
        && !HasBlockingMenu
        && !IsFestival
        && !CloseAlreadyAttempted;
}

internal static class AutoClosePopupPolicy
{
    public const int DefaultDelayTicks = 30;

    public static AutoClosePopupDecision Decide(
        AutoClosePopupConditions conditions,
        int visibleTicks,
        int requiredTicks = DefaultDelayTicks)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (!conditions.IsFishPopupVisible)
            return AutoClosePopupDecision.Reset;

        if (!conditions.IsEligible)
            return AutoClosePopupDecision.Wait;

        return visibleTicks >= Math.Max(0, requiredTicks)
            ? AutoClosePopupDecision.Close
            : AutoClosePopupDecision.Wait;
    }
}
