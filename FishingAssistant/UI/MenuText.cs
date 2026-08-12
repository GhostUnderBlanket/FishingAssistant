using Microsoft.Xna.Framework.Graphics;

namespace FishingAssistant.UI;

internal static class MenuText
{
    public static string Fit(string text, SpriteFont font, float maximumWidth)
    {
        if (maximumWidth <= 0)
            return "";
        if (font.MeasureString(text).X <= maximumWidth)
            return text;

        const string ellipsis = "...";
        if (font.MeasureString(ellipsis).X > maximumWidth)
            return "";

        int length = text.Length;
        while (length > 0 && font.MeasureString(text[..length] + ellipsis).X > maximumWidth)
            length--;

        return text[..length].TrimEnd() + ellipsis;
    }
}
