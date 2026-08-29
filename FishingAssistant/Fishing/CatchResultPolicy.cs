using FishingAssistant.Configuration;

namespace FishingAssistant.Fishing;

internal sealed record CatchResultConditions(
    int VanillaFishSize,
    int MaximumFishSize,
    int VanillaFishQuality,
    bool VanillaPerfect,
    int VanillaFishCount,
    int PreferredFishCount,
    FishAmountBehavior FishAmountBehavior,
    FishQualityPreference PreferredFishQuality,
    FishQualityBehavior FishQualityBehavior,
    bool AlwaysPerfect,
    bool AlwaysMaximumFishSize,
    bool IsFish,
    bool IsFestivalFishing,
    bool IsFromFishPond,
    bool IsBossFish,
    bool UsesChallengeBait);

internal sealed record CatchResultDecision(
    int FishSize,
    int FishQuality,
    bool IsPerfect,
    int FishCount,
    bool WasChanged);

internal static class CatchResultPolicy
{
    public static CatchResultDecision Decide(CatchResultConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        int fishSize = conditions.VanillaFishSize;
        int fishQuality = conditions.VanillaFishQuality;
        bool isPerfect = conditions.VanillaPerfect;
        int fishCount = conditions.VanillaFishCount;

        // Festival scoring and fish-pond rewards are owned by specialized vanilla
        // flows. Keep their complete result intact instead of treating them as a
        // normal rod catch.
        if (conditions.IsFestivalFishing || conditions.IsFromFishPond)
            return CreateDecision(conditions, fishSize, fishQuality, isPerfect, fishCount);

        if (conditions.IsFish)
        {
            if (conditions.AlwaysMaximumFishSize && conditions.MaximumFishSize > 0)
                fishSize = GetLargestFishSize(conditions.MaximumFishSize);

            if (conditions.PreferredFishQuality != FishQualityPreference.Any)
            {
                fishQuality = conditions.FishQualityBehavior switch
                {
                    FishQualityBehavior.Minimum => Math.Max(fishQuality, (int)conditions.PreferredFishQuality),
                    FishQualityBehavior.Fixed => (int)conditions.PreferredFishQuality,
                    _ => fishQuality
                };
            }

            if (conditions.AlwaysPerfect)
                isPerfect = true;

            // Preserve Challenge Bait and the one-fish legendary rule. Ordinary
            // multi-catches can use a minimum or a fixed configured quantity.
            if (!conditions.IsBossFish
                && !conditions.UsesChallengeBait)
            {
                int preferredCount = Math.Clamp(conditions.PreferredFishCount, 1, 3);
                fishCount = conditions.FishAmountBehavior switch
                {
                    FishAmountBehavior.Minimum => Math.Max(fishCount, preferredCount),
                    FishAmountBehavior.Fixed => preferredCount,
                    _ => fishCount
                };
            }
        }

        return CreateDecision(conditions, fishSize, fishQuality, isPerfect, fishCount);
    }

    internal static int GetLargestFishSize(int vanillaMaximumFishSize)
    {
        // BobberBar starts its size roll at min + (max - min) * roll and then adds
        // one. At a full roll, that yields max + 1; using max itself can be reduced
        // by vanilla as a near-perfect catch instead of being the largest possible fish.
        return vanillaMaximumFishSize < int.MaxValue
            ? vanillaMaximumFishSize + 1
            : vanillaMaximumFishSize;
    }

    private static CatchResultDecision CreateDecision(
        CatchResultConditions conditions,
        int fishSize,
        int fishQuality,
        bool isPerfect,
        int fishCount)
    {
        return new CatchResultDecision(
            fishSize,
            fishQuality,
            isPerfect,
            fishCount,
            fishSize != conditions.VanillaFishSize
                || fishQuality != conditions.VanillaFishQuality
                || isPerfect != conditions.VanillaPerfect
                || fishCount != conditions.VanillaFishCount);
    }
}
