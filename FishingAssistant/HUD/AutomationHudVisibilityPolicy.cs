namespace FishingAssistant.HUD;

internal sealed record AutomationHudVisibilityConditions(
    bool DisplayHud,
    bool HasBlockingMenu,
    bool IsEvent,
    bool IsFestival,
    bool HasUnsupportedMinigame,
    bool IsSupportedFestivalFishing);

internal static class AutomationHudVisibilityPolicy
{
    public static bool ShouldDraw(AutomationHudVisibilityConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        return (conditions.DisplayHud || conditions.IsSupportedFestivalFishing)
            && !conditions.HasBlockingMenu
            && (!conditions.IsEvent || conditions.IsFestival)
            && !conditions.HasUnsupportedMinigame;
    }
}
