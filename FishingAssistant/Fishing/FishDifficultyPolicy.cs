namespace FishingAssistant.Fishing;

internal sealed record FishDifficultyConditions(
    float VanillaDifficulty,
    float Multiplier,
    int Additive);

internal sealed record FishDifficultyDecision(
    float VanillaDifficulty,
    float AdjustedDifficulty,
    bool WasChanged);

internal static class FishDifficultyPolicy
{
    public static FishDifficultyDecision Decide(FishDifficultyConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        float vanilla = float.IsFinite(conditions.VanillaDifficulty)
            ? Math.Max(0f, conditions.VanillaDifficulty)
            : 0f;
        float multiplier = float.IsFinite(conditions.Multiplier)
            ? Math.Max(0f, conditions.Multiplier)
            : 1f;
        float adjusted = vanilla * multiplier + conditions.Additive;
        if (!float.IsFinite(adjusted))
            adjusted = vanilla;
        adjusted = Math.Max(0f, adjusted);

        return new FishDifficultyDecision(
            vanilla,
            adjusted,
            !adjusted.Equals(vanilla));
    }
}
