using FishingAssistant.Configuration;
using FishingAssistant.Runtime;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace FishingAssistant.Fishing;

internal sealed class BobberBarAdapter(BobberBar bar)
{
    public object Identity => bar;

    public static BobberBarAdapter? ForCurrentScreen()
    {
        return Game1.activeClickableMenu is BobberBar bar
            ? new BobberBarAdapter(bar)
            : null;
    }

    public MinigameControlConditions ReadConditions(
        bool automationEnabled,
        bool autoPlayEnabled,
        AutomationState state,
        float targetPosition)
    {
        return new MinigameControlConditions(
            automationEnabled,
            autoPlayEnabled,
            state,
            !bar.fadeIn && !bar.fadeOut && !bar.handledFishResult,
            targetPosition,
            bar.bobberBarPos,
            bar.bobberBarHeight
        );
    }

    public TreasureTargetConditions ReadTreasureConditions(
        bool assistanceActive,
        bool treasureTargetingEnabled,
        bool wasTargetingTreasure)
    {
        return new TreasureTargetConditions(
            assistanceActive,
            treasureTargetingEnabled,
            bar.treasure,
            bar.treasureCaught,
            bar.treasureScale,
            bar.distanceFromCatching,
            wasTargetingTreasure,
            bar.bobberPosition,
            bar.treasurePosition
        );
    }

    public TreasureChanceConditions ReadTreasureChanceConditions(ModConfig config)
    {
        return new TreasureChanceConditions(
            config.TreasureChance,
            config.GoldenTreasureChance,
            bar.treasure,
            bar.goldenTreasure,
            Game1.isFestival() || Game1.currentMinigame is FishingGame
        );
    }

    public FishDifficultyDecision ApplyDifficulty(ModConfig config)
    {
        FishDifficultyDecision decision = FishDifficultyPolicy.Decide(new FishDifficultyConditions(
            bar.difficulty,
            config.FishDifficultyMultiplier,
            config.FishDifficultyAdditive));
        bar.difficulty = decision.AdjustedDifficulty;

        // The constructor derives the initial target from difficulty. Keep it aligned
        // while the menu is still fading in, before normal fish motion begins.
        if (decision.WasChanged && bar.fadeIn)
        {
            bar.bobberTargetPosition = Math.Clamp(
                (100f - decision.AdjustedDifficulty) / 100f * 548f,
                -1f,
                548f);
        }

        return decision;
    }

    public SkipMinigameConditions ReadSkipMinigameConditions(SkipMinigameBehavior behavior)
    {
        string? qualifiedFishId = ItemRegistry.GetMetadata(bar.whichFish)?.QualifiedItemId;
        bool wasCaught = qualifiedFishId is not null
            && Game1.player.fishCaught.TryGetValue(qualifiedFishId, out int[]? catchData)
            && catchData is { Length: > 0 }
            && catchData[0] > 0;

        return new SkipMinigameConditions(
            behavior,
            !bar.fadeIn && !bar.fadeOut && !bar.handledFishResult && bar.distanceFromCatching < 1f,
            wasCaught,
            Game1.isFestival(),
            Game1.currentMinigame is FishingGame
        );
    }

    public void ApplyTreasureChance(TreasureChanceDecision decision)
    {
        bar.treasure = decision.HasTreasure;
        bar.goldenTreasure = decision.IsGoldenTreasure;
    }

    public void SetBarSpeed(float speed)
    {
        bar.bobberBarSpeed = speed;
    }

    public void CompleteMinigame(bool collectTreasure)
    {
        if (bar.treasure && collectTreasure)
            bar.treasureCaught = true;

        bar.distanceFromCatching = 1f;
    }
}
