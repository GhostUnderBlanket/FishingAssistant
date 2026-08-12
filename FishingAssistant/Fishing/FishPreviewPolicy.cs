namespace FishingAssistant.Fishing;

internal sealed record FishPreviewConditions(
    bool PreviewEnabled,
    bool IsMinigameReady,
    bool WasCaught,
    bool IsLegendary,
    bool RevealUncaughtFish,
    bool RevealLegendaryFish,
    bool ShowFishName,
    bool ShowTreasure,
    bool HasTreasure,
    bool IsGoldenTreasure);

internal sealed record FishPreviewDecision(
    bool ShouldDraw,
    bool RevealFish,
    bool ShowFishName,
    bool ShowTreasure,
    bool IsGoldenTreasure);

internal static class FishPreviewPolicy
{
    public static FishPreviewDecision Decide(FishPreviewConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (!conditions.PreviewEnabled || !conditions.IsMinigameReady)
            return new(false, false, false, false, false);

        bool revealFish = conditions.WasCaught
            || conditions.RevealUncaughtFish
            || (conditions.IsLegendary && conditions.RevealLegendaryFish);

        return new FishPreviewDecision(
            ShouldDraw: true,
            RevealFish: revealFish,
            ShowFishName: conditions.ShowFishName,
            ShowTreasure: conditions.ShowTreasure && conditions.HasTreasure,
            IsGoldenTreasure: conditions.ShowTreasure
                && conditions.HasTreasure
                && conditions.IsGoldenTreasure);
    }
}
