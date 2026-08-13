using FishingAssistant.Configuration;
using Microsoft.Xna.Framework;

namespace FishingAssistant.HUD;

internal sealed record AutomationHudLayoutConditions(
    int ViewportWidth,
    int ViewportHeight,
    HudPosition Position,
    int ToolbarWidth,
    float ToolbarOpacity,
    bool IsFishingMinigame,
    bool IsFestival,
    bool IsToolbarAtTop);

internal static class AutomationHudLayout
{
    public const int BoxSize = 96;
    public const int IconSize = 20;
    public const int BadgeSize = 32;
    public const int BadgeInset = 4;
    public const int ScreenMargin = 8;
    public const int ToolbarGap = 2;
    public const float IconScale = 2f;

    public static Rectangle Place(AutomationHudLayoutConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        int width = Math.Max(1, conditions.ViewportWidth);
        int height = Math.Max(1, conditions.ViewportHeight);
        int boxSize = Math.Min(BoxSize, Math.Min(width, height));
        bool canFollowToolbar = conditions.ToolbarOpacity > 0f
            && conditions.ToolbarWidth > 0
            && !conditions.IsFishingMinigame
            && !conditions.IsFestival;
        int direction = conditions.Position == HudPosition.Left ? -1 : 1;
        int toolbarOffset = canFollowToolbar
            ? direction * (conditions.ToolbarWidth / 2 + ToolbarGap)
            : 0;
        int x = width / 2 + toolbarOffset - boxSize / 2;
        int y = conditions.IsToolbarAtTop
            ? ScreenMargin
            : height - ScreenMargin - boxSize;

        x = Math.Clamp(x, 0, Math.Max(0, width - boxSize));
        y = Math.Clamp(y, 0, Math.Max(0, height - boxSize));
        return new Rectangle(x, y, boxSize, boxSize);
    }

    public static Rectangle PlaceBadge(Rectangle panelBounds)
    {
        int size = Math.Min(BadgeSize, Math.Min(panelBounds.Width, panelBounds.Height));
        int inset = Math.Min(BadgeInset, Math.Max(0, (Math.Min(panelBounds.Width, panelBounds.Height) - size) / 2));
        return new Rectangle(
            panelBounds.Right - inset - size,
            panelBounds.Bottom - inset - size,
            size,
            size);
    }
}
