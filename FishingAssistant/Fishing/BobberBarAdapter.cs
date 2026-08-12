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

    public void ApplyTreasureChance(TreasureChanceDecision decision)
    {
        bar.treasure = decision.HasTreasure;
        bar.goldenTreasure = decision.IsGoldenTreasure;
    }

    public void SetBarSpeed(float speed)
    {
        bar.bobberBarSpeed = speed;
    }
}
