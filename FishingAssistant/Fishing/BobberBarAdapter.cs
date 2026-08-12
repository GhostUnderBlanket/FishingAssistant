using FishingAssistant.Runtime;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.Fishing;

internal sealed class BobberBarAdapter(BobberBar bar)
{
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

    public void SetBarSpeed(float speed)
    {
        bar.bobberBarSpeed = speed;
    }
}
