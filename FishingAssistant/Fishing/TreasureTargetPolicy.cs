namespace FishingAssistant.Fishing;

internal enum MinigameTarget
{
    Fish,
    Treasure
}

internal sealed record TreasureTargetConditions(
    bool AssistanceActive,
    bool TreasureTargetingEnabled,
    bool TreasureAvailable,
    bool TreasureCaught,
    float TreasureScale,
    float CatchProgress,
    bool WasTargetingTreasure,
    float FishPosition,
    float TreasurePosition);

internal sealed record TreasureTargetDecision(
    MinigameTarget Target,
    float Position,
    bool IsTargetingTreasure);

internal static class TreasureTargetPolicy
{
    private const float BeginTreasureProgress = 0.9f;
    private const float AbandonTreasureProgress = 0.35f;

    public static TreasureTargetDecision Decide(TreasureTargetConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        bool canTargetTreasure = conditions.AssistanceActive
            && conditions.TreasureTargetingEnabled
            && conditions.TreasureAvailable
            && !conditions.TreasureCaught
            && conditions.TreasureScale >= 1f;
        bool continueTreasure = conditions.WasTargetingTreasure
            && conditions.CatchProgress > AbandonTreasureProgress;
        bool beginTreasure = conditions.CatchProgress >= BeginTreasureProgress;
        if (canTargetTreasure && (continueTreasure || beginTreasure))
        {
            return new TreasureTargetDecision(
                MinigameTarget.Treasure,
                conditions.TreasurePosition,
                IsTargetingTreasure: true
            );
        }

        return new TreasureTargetDecision(
            MinigameTarget.Fish,
            conditions.FishPosition,
            IsTargetingTreasure: false
        );
    }
}
