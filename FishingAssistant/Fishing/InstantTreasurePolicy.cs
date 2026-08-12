namespace FishingAssistant.Fishing;

internal enum InstantTreasureDecision
{
    Wait,
    Capture
}

internal sealed record InstantTreasureConditions(
    bool Enabled,
    bool IsMinigameActive,
    bool TreasureAvailable,
    bool TreasureCaught,
    float TreasureScale,
    bool IsFestivalFishing);

internal static class InstantTreasurePolicy
{
    public static InstantTreasureDecision Decide(InstantTreasureConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        return conditions.Enabled
            && conditions.IsMinigameActive
            && conditions.TreasureAvailable
            && !conditions.TreasureCaught
            && conditions.TreasureScale >= 1f
            && !conditions.IsFestivalFishing
                ? InstantTreasureDecision.Capture
                : InstantTreasureDecision.Wait;
    }
}
