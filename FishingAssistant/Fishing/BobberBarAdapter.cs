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
        AutomationState state)
    {
        return new MinigameControlConditions(
            automationEnabled,
            autoPlayEnabled,
            state,
            !bar.fadeIn && !bar.fadeOut && !bar.handledFishResult,
            bar.bobberPosition,
            bar.bobberBarPos,
            bar.bobberBarHeight
        );
    }

    public void SetBarSpeed(float speed)
    {
        bar.bobberBarSpeed = speed;
    }
}
