namespace FishingAssistant.UI.Controls;

using Microsoft.Xna.Framework;

internal static class MouseWheelAdjustment
{
    public static int GetDirection(int wheelDelta) => Math.Sign(wheelDelta);

    public static bool IsPointerOver(Rectangle selectorBounds, Point pointer) =>
        selectorBounds.Contains(pointer);
}
