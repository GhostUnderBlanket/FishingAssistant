using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.UI;

internal static class MenuTooltip
{
    internal const int PreferredTextWidth = 640;
    internal const int HorizontalSafePadding = 64;

    public static int GetTextWidth(int viewportWidth)
    {
        return Math.Max(1, Math.Min(PreferredTextWidth, viewportWidth - HorizontalSafePadding));
    }

    public static void Draw(SpriteBatch batch, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        int textWidth = GetTextWidth(Game1.uiViewport.Width);
        string wrapped = Game1.parseText(text, Game1.smallFont, textWidth);
        IClickableMenu.drawHoverText(batch, wrapped, Game1.smallFont);
    }
}
