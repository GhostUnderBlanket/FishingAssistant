using FishingAssistant.Configuration;

namespace FishingAssistant.Fishing;

internal sealed record TreasureChanceConditions(
    TreasureChanceBehavior TreasureBehavior,
    TreasureChanceBehavior GoldenBehavior,
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

        bool hasTreasure = conditions.TreasureBehavior switch
        {
            TreasureChanceBehavior.Always => true,
            TreasureChanceBehavior.Never => false,
            _ => conditions.VanillaTreasure
        };
        if (!hasTreasure)
            return new TreasureChanceDecision(false, false);

        bool isGolden = conditions.GoldenBehavior switch
        {
            TreasureChanceBehavior.Always => true,
            TreasureChanceBehavior.Never => false,
            _ => conditions.VanillaGoldenTreasure
        };
        return new TreasureChanceDecision(true, isGolden);
    }
}
