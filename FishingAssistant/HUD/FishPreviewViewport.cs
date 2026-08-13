using Microsoft.Xna.Framework;

namespace FishingAssistant.HUD;

internal static class FishPreviewViewport
{
    public static FishPreviewCoordinateSpace FromViewports(Rectangle gameViewport, Rectangle uiViewport)
    {
        int gameWidth = Math.Max(1, gameViewport.Width);
        int gameHeight = Math.Max(1, gameViewport.Height);
        int uiWidth = Math.Max(1, uiViewport.Width);
        int uiHeight = Math.Max(1, uiViewport.Height);
        return new FishPreviewCoordinateSpace(
            new Rectangle(0, 0, uiWidth, uiHeight),
            uiWidth / (float)gameWidth,
            uiHeight / (float)gameHeight);
    }
}

internal sealed record FishPreviewCoordinateSpace(Rectangle Viewport, float ScaleX, float ScaleY)
{
    public Rectangle ToUi(Rectangle gameBounds)
    {
        return new Rectangle(
            (int)MathF.Round(gameBounds.X * this.ScaleX),
            (int)MathF.Round(gameBounds.Y * this.ScaleY),
            Math.Max(1, (int)MathF.Round(gameBounds.Width * this.ScaleX)),
            Math.Max(1, (int)MathF.Round(gameBounds.Height * this.ScaleY)));
    }
}
