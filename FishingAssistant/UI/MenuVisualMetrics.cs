using Microsoft.Xna.Framework;

namespace FishingAssistant.UI;

internal static class MenuVisualMetrics
{
    public const int ControlHeight = 48;

    public const float ArrowScale = 1.75f;

    public static Rectangle ArrowSource { get; } = new(421, 459, 11, 12);

    public static Color InlineMessageBackground { get; } = new(255, 239, 190);

    public static Color InlineMessageAccent { get; } = new(166, 52, 32);

    public static Color InlineMessageText { get; } = new(82, 28, 22);

    public static Color DisabledControlOverlay { get; } = Color.Black * 0.38f;

    public static Color DisabledMessageBackground { get; } = new(215, 220, 217);

    public static Color DisabledMessageAccent { get; } = new(98, 106, 109);

    public static Color DisabledMessageText { get; } = new(48, 55, 58);

    public static int GetControlWidth(int availableWidth)
    {
        return Math.Clamp(availableWidth * 43 / 100, 152, 320);
    }

    public static int GetControlHeight(int availableHeight)
    {
        return Math.Min(ControlHeight, Math.Max(1, availableHeight - 4));
    }
}
