namespace FishingAssistant.Fishing;

internal enum InstantBiteDecision
{
    Wait,
    Trigger
}

internal sealed record InstantBiteConditions(
    bool InstantBiteEnabled,
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

        bool shouldTrigger = conditions.InstantBiteEnabled
            && conditions.IsFishing
            && !conditions.IsNibbling
            && conditions.HasPendingBiteTimer
            && !conditions.HasBlockingMenu
            && (!conditions.IsFestival || conditions.IsSupportedFishingMinigame);
        return shouldTrigger ? InstantBiteDecision.Trigger : InstantBiteDecision.Wait;
    }
}
