using FishingAssistant.Runtime;
using Microsoft.Xna.Framework;

namespace FishingAssistant.HUD;

internal static class AutomationHudVisualPolicy
{
    public static Color GetAutomationTint(bool enabled, AutomationState state)
    {
        if (!enabled)
            return Color.White * 0.2f;

        return state switch
        {
            AutomationState.Paused => Color.Gold,
            _ => Color.White
        };
    }
}
