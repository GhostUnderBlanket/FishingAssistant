using Microsoft.Xna.Framework;

namespace FishingAssistant.UI;

internal static class MenuVisualMetrics
{
    public const int ControlHeight = 48;

    public const float ArrowScale = 1.75f;

    public static Rectangle ArrowSource { get; } = new(421, 459, 11, 12);

    public static int GetControlWidth(int availableWidth)
    {
        return Math.Clamp(availableWidth * 43 / 100, 152, 320);
    }

    public static int GetControlHeight(int availableHeight)
    {
        return Math.Min(ControlHeight, Math.Max(1, availableHeight - 4));
    }
}
