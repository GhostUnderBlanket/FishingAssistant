using Microsoft.Xna.Framework;

namespace FishingAssistant.HUD;

internal static class FishPreviewLayout
{
    public const int EdgeMargin = 12;
    // BobberBar.width covers its logical menu area, while the vanilla frame artwork
    // extends roughly another tile beyond it. Keep the preview clear of that artwork.
    public const int AnchorGap = 64;
    public const int ForcedLeftGap = 24;
    public const int MinimumWidth = 112;

    public static Rectangle Place(
        Rectangle viewport,
        Rectangle anchor,
        Point desiredSize,
        bool forceLeft = false)
    {
        int maximumWidth = Math.Max(1, viewport.Width - EdgeMargin * 2);
        int width = Math.Clamp(desiredSize.X, 1, maximumWidth);
        int height = Math.Clamp(desiredSize.Y, 1, Math.Max(1, viewport.Height - EdgeMargin * 2));

        int horizontalGap = forceLeft ? ForcedLeftGap : AnchorGap;
        int rightSpace = viewport.Right - EdgeMargin - anchor.Right - horizontalGap;
        int leftSpace = anchor.Left - horizontalGap - (viewport.Left + EdgeMargin);
        bool placeRight = !forceLeft && (rightSpace >= width || rightSpace >= leftSpace);
        int x = placeRight
            ? anchor.Right + horizontalGap
            : anchor.Left - horizontalGap - width;

        x = Math.Clamp(x, viewport.Left + EdgeMargin, viewport.Right - EdgeMargin - width);
        int y = Math.Clamp(anchor.Top, viewport.Top + EdgeMargin, viewport.Bottom - EdgeMargin - height);
        return new Rectangle(x, y, width, height);
    }
}
