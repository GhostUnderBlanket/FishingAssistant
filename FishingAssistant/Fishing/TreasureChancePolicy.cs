using FishingAssistant.Configuration;

namespace FishingAssistant.Fishing;

internal sealed record TreasureChanceConditions(
    int TreasureChancePercent,
    int GoldenChancePercent,
    double AdjustedTreasureChance,
    double AdjustedGoldenChance,
    double TreasureRoll,
    double GoldenRoll,
    bool VanillaTreasure,
    bool VanillaGoldenTreasure,
    bool IsFestivalFishing);

internal sealed record TreasureChanceDecision(bool HasTreasure, bool IsGoldenTreasure);

internal static class TreasureChancePolicy
{
    public static TreasureChanceDecision Decide(TreasureChanceConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (conditions.IsFestivalFishing)
            return new TreasureChanceDecision(conditions.VanillaTreasure, conditions.VanillaGoldenTreasure);

        bool hasTreasure = conditions.TreasureChancePercent switch
        {
            <= 0 => false,
            >= 100 => true,
            15 => conditions.VanillaTreasure,
            _ => conditions.TreasureRoll < Math.Clamp(conditions.AdjustedTreasureChance, 0d, 1d)
        };
        if (!hasTreasure)
            return new TreasureChanceDecision(false, false);

        bool isGolden = conditions.GoldenChancePercent switch
        {
            <= 0 => false,
            >= 100 => true,
            25 => conditions.VanillaGoldenTreasure,
            _ => conditions.GoldenRoll < Math.Clamp(conditions.AdjustedGoldenChance, 0d, 1d)
        };
        return new TreasureChanceDecision(true, isGolden);
    }
}
