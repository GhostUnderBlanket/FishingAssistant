using FishingAssistant.Runtime;

namespace FishingAssistant.HUD;

internal static class AutomationHudVisualPolicy
{
    public static AutomationHudVisual GetVisual(
        bool enabled,
        AutomationState state,
        AutomationTransitionReason reason)
    {
        if (reason == AutomationTransitionReason.LateNight)
            return new(AutomationHudBadge.LateNight);

        if (reason == AutomationTransitionReason.LowEnergy)
            return new(AutomationHudBadge.LowEnergy);

        if (reason == AutomationTransitionReason.TimedOut)
            return new(AutomationHudBadge.Warning);

        if (!enabled)
            return new(AutomationHudBadge.Disabled);

        if (state == AutomationState.Paused || reason == AutomationTransitionReason.MenuInterrupted)
            return new(AutomationHudBadge.Paused);

        if (reason == AutomationTransitionReason.Recovered)
            return new(AutomationHudBadge.Recovered);

        if (state != AutomationState.Idle)
            return new(AutomationHudBadge.Working);

        return new(AutomationHudBadge.None);
    }
}

internal sealed record AutomationHudVisual(AutomationHudBadge Badge);

internal enum AutomationHudBadge
{
    None,
    Disabled,
    Paused,
    LateNight,
    LowEnergy,
    Warning,
    Recovered,
    Working
}
