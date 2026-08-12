using FishingAssistant.Runtime;

namespace FishingAssistant.Fishing;

internal enum AutoCastDecision
{
    Reset,
    Wait,
    Cast
}

internal sealed record AutoCastConditions(
    bool AutomationEnabled,
    bool AutoCastEnabled,
    AutomationState State,
    bool IsPlayerFree,
    bool CanMove,
    bool IsMoving,
    bool HasEnoughStamina,
    bool IsFestival,
    bool IsTargetFishable);

internal static class AutoCastPolicy
{
    public static AutoCastDecision Decide(
        AutoCastConditions conditions,
        int readyTicks,
        int requiredReadyTicks)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        bool safe = conditions.AutomationEnabled
            && conditions.AutoCastEnabled
            && conditions.State == AutomationState.Ready
            && conditions.IsPlayerFree
            && conditions.CanMove
            && !conditions.IsMoving
            && conditions.HasEnoughStamina
            && !conditions.IsFestival
            && conditions.IsTargetFishable;
        if (!safe)
            return AutoCastDecision.Reset;

        int required = Math.Max(0, requiredReadyTicks);
        return readyTicks + 1 >= required
            ? AutoCastDecision.Cast
            : AutoCastDecision.Wait;
    }
}
