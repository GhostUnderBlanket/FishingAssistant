using FishingAssistant.Configuration;
using FishingAssistant.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.HUD;

internal sealed class AutomationHudRenderer(Func<string, string> translate)
{
    public void Draw(SpriteBatch batch, AutomationSession session, ModConfig config)
    {
        if (!Game1.displayHUD || Game1.eventUp || Game1.currentMinigame is not null)
            return;

        string enabled = translate(session.IsEnabled ? "hud.enabled" : "hud.disabled");
        string state = translate($"hud.state.{session.State.ToString().ToLowerInvariant()}");
        string treasure = translate(session.IsTreasureTargetingEnabled ? "hud.on" : "hud.off");
        string text = $"{translate("hud.title")}: {enabled} | {state} | " +
                      $"{translate("hud.treasure")}: {treasure}";
        Vector2 textSize = Game1.smallFont.MeasureString(text);
        int width = (int)textSize.X + 28;
        int height = Game1.smallFont.LineSpacing + 20;
        int x = config.ModStatusPosition == HudPosition.Left
            ? 20
            : Math.Max(20, Game1.uiViewport.Width - width - 20);
        const int y = 20;

        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            x, y, width, height, Color.White * 0.92f);
        Utility.drawTextWithShadow(batch, text, Game1.smallFont,
            new Vector2(x + 14, y + 10), Game1.textColor);
    }
}
