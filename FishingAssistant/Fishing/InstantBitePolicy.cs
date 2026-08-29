namespace FishingAssistant.Fishing;

internal enum InstantBiteDecision
{
    Wait,
    ApplyWaitingTime
}

internal sealed record InstantBiteConditions(
    int WaitingTimePercent,
    bool WaitingTimeAlreadyApplied,
    bool IsFishing,
    bool IsNibbling,
    bool HasPendingBiteTimer,
    bool HasBlockingMenu,
    bool IsFestival,
    bool IsSupportedFishingMinigame);

internal static class InstantBitePolicy
{
    public static InstantBiteDecision Decide(InstantBiteConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        bool shouldApply = !conditions.WaitingTimeAlreadyApplied
            && conditions.IsFishing
            && !conditions.IsNibbling
            && conditions.HasPendingBiteTimer
            && !conditions.HasBlockingMenu
            && (!conditions.IsFestival || conditions.IsSupportedFishingMinigame);
        return shouldApply ? InstantBiteDecision.ApplyWaitingTime : InstantBiteDecision.Wait;
    }
}
