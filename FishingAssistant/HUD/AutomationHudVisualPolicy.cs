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
            return new(Color.White * 0.2f, AutomationHudBadge.LateNight);

        if (reason == AutomationTransitionReason.LowEnergy)
            return new(Color.White * 0.2f, AutomationHudBadge.LowEnergy);

        if (reason == AutomationTransitionReason.TimedOut)
            return new(Color.White * 0.2f, AutomationHudBadge.Warning);

        if (!enabled)
            return new(Color.White * 0.2f, AutomationHudBadge.Disabled);

        if (state == AutomationState.Paused || reason == AutomationTransitionReason.MenuInterrupted)
            return new(Color.Gold, AutomationHudBadge.Paused);

        if (reason == AutomationTransitionReason.Recovered)
            return new(Color.LightGreen, AutomationHudBadge.Recovered);

        return new(Color.White, AutomationHudBadge.None);
    }
}

internal sealed record AutomationHudVisual(
    Color IconTint,
    AutomationHudBadge Badge);

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
