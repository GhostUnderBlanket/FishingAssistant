namespace FishingAssistant.UI;

internal static class ResetConfirmationLayout
{
    internal const int MaximumTextWidth = 640;
    internal const int DialogChromeAndMargins = 144;

    public static int GetTextWidth(int viewportWidth)
    {
        return Math.Max(1, Math.Min(MaximumTextWidth, viewportWidth - DialogChromeAndMargins));
    }
}
