using FishingAssistant.Configuration;
using FishingAssistant.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace FishingAssistant.HUD;

internal sealed class AutomationHudRenderer
{
    private static readonly Rectangle FishingIconSource = new(20, 428, 10, 10);

    public void Draw(SpriteBatch batch, AutomationSession session, ModConfig config)
    {
        if (!Game1.displayHUD
            || (Game1.eventUp && !Game1.isFestival())
            || (Game1.currentMinigame is not null && Game1.currentMinigame is not FishingGame))
        {
            return;
        }

        Toolbar? toolbar = Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
        Rectangle bounds = AutomationHudLayout.Place(new AutomationHudLayoutConditions(
            Game1.uiViewport.Width,
            Game1.uiViewport.Height,
            config.ModStatusPosition,
            toolbar?.width ?? 0,
            toolbar?.transparency ?? 0f,
            IsFishingMinigame: Game1.currentMinigame is FishingGame,
            IsFestival: Game1.isFestival(),
            IsToolbarAtTop: IsToolbarAtTop()));
        float opacity = toolbar is null ? 1f : Math.Clamp(toolbar.transparency, 0.33f, 1f);
        AutomationHudVisual visual = AutomationHudVisualPolicy.GetVisual(
            session.IsEnabled,
            session.State,
            session.LastReason);

        IClickableMenu.drawTextureBox(
            batch,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            Color.White * opacity,
            drawShadow: false);

        DrawIcon(
            batch,
            FishingIconSource,
            new Vector2(bounds.Center.X - AutomationHudLayout.IconSize / 2,
                bounds.Center.Y - AutomationHudLayout.IconSize / 2),
            visual.IconTint * opacity);

        if (visual.Badge != AutomationHudBadge.None)
            DrawBadge(batch, AutomationHudLayout.PlaceBadge(bounds), visual, opacity);

        return;

        bool IsToolbarAtTop()
        {
            if (Game1.options.pinToolbarToggle)
                return false;

            Vector2 playerPosition = Game1.GlobalToLocal(Game1.viewport, Game1.player.StandingPixel.ToVector2());
            return playerPosition.Y > Game1.uiViewport.Height / 2f + 64f;
        }
    }

    private static void DrawIcon(SpriteBatch batch, Rectangle source, Vector2 position, Color tint)
    {
        batch.Draw(
            Game1.mouseCursors,
            position,
            source,
            tint,
            0f,
            Vector2.Zero,
            AutomationHudLayout.IconScale,
            SpriteEffects.None,
            1f);
    }

    private static void DrawBadge(
        SpriteBatch batch,
        Rectangle bounds,
        AutomationHudVisual visual,
        float opacity)
    {
        if (bounds.Width < 8 || bounds.Height < 8)
            return;

        DrawRectangle(batch, bounds, Color.Black * (0.8f * opacity));
        Rectangle inner = new(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
        DrawRectangle(batch, inner, visual.BadgeColor * opacity);

        int pixel = Math.Max(1, inner.Width / 7);
        int glyphSize = pixel * 5;
        int x = inner.Center.X - glyphSize / 2;
        int y = inner.Center.Y - glyphSize / 2;
        Color glyphColor = (visual.Badge == AutomationHudBadge.LateNight
            ? Color.LightYellow
            : Color.White) * opacity;

        switch (visual.Badge)
        {
            case AutomationHudBadge.Disabled:
                DrawCross(batch, x, y, pixel, glyphColor);
                break;
            case AutomationHudBadge.Paused:
                DrawRectangle(batch, new Rectangle(x, y, pixel, glyphSize), glyphColor);
                DrawRectangle(batch, new Rectangle(x + pixel * 4, y, pixel, glyphSize), glyphColor);
                break;
            case AutomationHudBadge.LateNight:
                DrawRectangle(batch, new Rectangle(x, y, pixel * 4, pixel), glyphColor);
                DrawRectangle(batch, new Rectangle(x, y, pixel, glyphSize), glyphColor);
                DrawRectangle(batch, new Rectangle(x, y + pixel * 4, pixel * 4, pixel), glyphColor);
                DrawRectangle(batch, new Rectangle(x + pixel * 3, y + pixel, pixel, pixel), glyphColor);
                DrawRectangle(batch, new Rectangle(x + pixel * 3, y + pixel * 3, pixel, pixel), glyphColor);
                break;
            case AutomationHudBadge.LowEnergy:
                DrawRectangle(batch, new Rectangle(x + pixel * 2, y, pixel * 2, pixel * 2), glyphColor);
                DrawRectangle(batch, new Rectangle(x + pixel, y + pixel * 2, pixel * 3, pixel), glyphColor);
                DrawRectangle(batch, new Rectangle(x + pixel, y + pixel * 3, pixel * 2, pixel * 2), glyphColor);
                break;
            case AutomationHudBadge.Warning:
                DrawRectangle(batch, new Rectangle(x + pixel * 2, y, pixel, pixel * 3), glyphColor);
                DrawRectangle(batch, new Rectangle(x + pixel * 2, y + pixel * 4, pixel, pixel), glyphColor);
                break;
            case AutomationHudBadge.Recovered:
                DrawRectangle(batch, new Rectangle(x, y + pixel * 2, pixel * 2, pixel), glyphColor);
                DrawRectangle(batch, new Rectangle(x + pixel, y + pixel * 3, pixel, pixel), glyphColor);
                DrawRectangle(batch, new Rectangle(x + pixel * 2, y + pixel * 2, pixel, pixel), glyphColor);
                DrawRectangle(batch, new Rectangle(x + pixel * 3, y + pixel, pixel, pixel), glyphColor);
                DrawRectangle(batch, new Rectangle(x + pixel * 4, y, pixel, pixel), glyphColor);
                break;
        }
    }

    private static void DrawCross(SpriteBatch batch, int x, int y, int pixel, Color color)
    {
        for (int index = 0; index < 5; index++)
        {
            DrawRectangle(batch, new Rectangle(x + index * pixel, y + index * pixel, pixel, pixel), color);
            DrawRectangle(batch, new Rectangle(x + (4 - index) * pixel, y + index * pixel, pixel, pixel), color);
        }
    }

    private static void DrawRectangle(SpriteBatch batch, Rectangle bounds, Color color)
    {
        batch.Draw(Game1.staminaRect, bounds, color);
    }
}
