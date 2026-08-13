using FishingAssistant.Runtime;
using Microsoft.Xna.Framework;

namespace FishingAssistant.HUD;

internal static class AutomationHudVisualPolicy
{
    public static AutomationHudVisual GetVisual(
        bool enabled,
        AutomationState state,
        AutomationTransitionReason reason)
    {
        if (reason == AutomationTransitionReason.LateNight)
            return new(Color.White * 0.2f, AutomationHudBadge.LateNight, Color.MidnightBlue);

        if (reason == AutomationTransitionReason.LowEnergy)
            return new(Color.White * 0.2f, AutomationHudBadge.LowEnergy, Color.DarkOrange);

        if (reason == AutomationTransitionReason.TimedOut)
            return new(Color.White * 0.2f, AutomationHudBadge.Warning, Color.IndianRed);

        if (!enabled)
            return new(Color.White * 0.2f, AutomationHudBadge.Disabled, Color.DarkRed);

        if (state == AutomationState.Paused || reason == AutomationTransitionReason.MenuInterrupted)
            return new(Color.Gold, AutomationHudBadge.Paused, Color.Goldenrod);

        if (reason == AutomationTransitionReason.Recovered)
            return new(Color.LightGreen, AutomationHudBadge.Recovered, Color.SeaGreen);

        return new(Color.White, AutomationHudBadge.None, Color.Transparent);
    }
}

internal sealed record AutomationHudVisual(
    Color IconTint,
    AutomationHudBadge Badge,
    Color BadgeColor);

internal enum AutomationHudBadge
{
    None,
    Disabled,
    Paused,
    LateNight,
    LowEnergy,
    Warning,
    Recovered
}
