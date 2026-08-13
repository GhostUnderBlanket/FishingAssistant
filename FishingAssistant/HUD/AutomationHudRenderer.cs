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
        if (bounds.Width < 16 || bounds.Height < 16)
            return;

        int emoteIndex = visual.Badge switch
        {
            AutomationHudBadge.Disabled => Character.xEmote,
            AutomationHudBadge.Paused => Character.pauseEmote,
            AutomationHudBadge.LateNight => Character.sleepEmote,
            AutomationHudBadge.LowEnergy => Character.sadEmote,
            AutomationHudBadge.Warning => Character.exclamationEmote,
            AutomationHudBadge.Recovered => Character.happyEmote,
            _ => -1
        };
        if (emoteIndex < 0)
            return;

        const int sourceSize = 16;
        Rectangle source = new(
            emoteIndex * sourceSize % Game1.emoteSpriteSheet.Width,
            emoteIndex * sourceSize / Game1.emoteSpriteSheet.Width * sourceSize,
            sourceSize,
            sourceSize);
        batch.Draw(
            Game1.emoteSpriteSheet,
            bounds,
            source,
            Color.White * opacity,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            1f);
    }
}
