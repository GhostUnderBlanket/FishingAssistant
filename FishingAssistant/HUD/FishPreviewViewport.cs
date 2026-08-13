using Microsoft.Xna.Framework;

namespace FishingAssistant.HUD;

internal static class FishPreviewViewport
{
    public static Rectangle FromGameViewport(Rectangle gameViewport)
    {
        return new Rectangle(0, 0, Math.Max(1, gameViewport.Width), Math.Max(1, gameViewport.Height));
    }
}
