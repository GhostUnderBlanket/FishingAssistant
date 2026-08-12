using Microsoft.Xna.Framework;

namespace FishingAssistant.HUD;

internal static class FishPreviewLayout
{
    public const int EdgeMargin = 12;
    public const int AnchorGap = 20;
    public const int MinimumWidth = 112;

    public static Rectangle Place(Rectangle viewport, Rectangle anchor, Point desiredSize)
    {
        int maximumWidth = Math.Max(1, viewport.Width - EdgeMargin * 2);
        int width = Math.Clamp(desiredSize.X, 1, maximumWidth);
        int height = Math.Clamp(desiredSize.Y, 1, Math.Max(1, viewport.Height - EdgeMargin * 2));

        int rightSpace = viewport.Right - EdgeMargin - anchor.Right - AnchorGap;
        int leftSpace = anchor.Left - AnchorGap - (viewport.Left + EdgeMargin);
        bool placeRight = rightSpace >= width || rightSpace >= leftSpace;
        int x = placeRight
            ? anchor.Right + AnchorGap
            : anchor.Left - AnchorGap - width;

        x = Math.Clamp(x, viewport.Left + EdgeMargin, viewport.Right - EdgeMargin - width);
        int y = Math.Clamp(anchor.Top, viewport.Top + EdgeMargin, viewport.Bottom - EdgeMargin - height);
        return new Rectangle(x, y, width, height);
    }
}
