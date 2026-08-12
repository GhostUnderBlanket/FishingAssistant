using FishingAssistant.Runtime;

namespace FishingAssistant.Fishing;

internal enum AutoHookDecision
{
    ResetAttempt,
    Wait,
    Hook
}

internal sealed record AutoHookConditions(
    bool AutomationEnabled,
    bool AutoHookEnabled,
    AutomationState State,
    bool IsNibbling,
    bool HookAlreadyAttempted,
    bool HasAutoHookEnchantment,
    bool HasBlockingMenu,
    bool IsFestival,
    bool IsSupportedFishingMinigame,
    bool IsHookSafe);

internal static class AutoHookPolicy
{
    public static AutoHookDecision Decide(AutoHookConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (!conditions.IsNibbling)
            return AutoHookDecision.ResetAttempt;

        bool shouldHook = conditions.AutomationEnabled
            && conditions.AutoHookEnabled
            && conditions.State == AutomationState.Hooking
            && !conditions.HookAlreadyAttempted
            && !conditions.HasAutoHookEnchantment
            && !conditions.HasBlockingMenu
            && (!conditions.IsFestival || conditions.IsSupportedFishingMinigame)
            && conditions.IsHookSafe;
        return shouldHook ? AutoHookDecision.Hook : AutoHookDecision.Wait;
    }
}
