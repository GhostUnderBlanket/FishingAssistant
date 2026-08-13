namespace FishingAssistant.UI.Controls;

internal static class MouseWheelAdjustment
{
    public static int GetDirection(int wheelDelta) => Math.Sign(wheelDelta);
}
