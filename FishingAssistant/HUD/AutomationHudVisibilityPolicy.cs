using FishingAssistant.Configuration;

namespace FishingAssistant.HUD;

internal sealed record AutomationHudVisibilityConditions(
    HudVisibilityMode Visibility,
    bool DisplayHud,
    bool HasBlockingMenu,
    bool IsEvent,
    bool IsFestival,
    bool HasUnsupportedMinigame,
    bool IsSupportedFestivalFishing,
    bool HasFishingRod,
    bool IsFishingActive);

internal static class AutomationHudVisibilityPolicy
{
    public static bool ShouldDraw(AutomationHudVisibilityConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        bool isVisibleForMode = conditions.Visibility switch
        {
            HudVisibilityMode.Always => true,
            HudVisibilityMode.WhileFishing => conditions.HasFishingRod
                || conditions.IsFishingActive
                || conditions.IsSupportedFestivalFishing,
            HudVisibilityMode.Hidden => false,
            _ => false
        };

        return isVisibleForMode
            && (conditions.DisplayHud || conditions.IsSupportedFestivalFishing)
            && !conditions.HasBlockingMenu
            && (!conditions.IsEvent || conditions.IsFestival)
            && !conditions.HasUnsupportedMinigame;
    }
}
