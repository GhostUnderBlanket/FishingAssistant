using FishingAssistant.Configuration;
using FishingAssistant.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace FishingAssistant.HUD;

internal sealed class AutomationHudRenderer
{
    private const string FallbackRodId = "(T)AdvancedIridiumRod";
    private static readonly Rectangle TreasureHunterSource = new(137, 412, 10, 11);

    public void Draw(SpriteBatch batch, AutomationSession session, ModConfig config)
    {
        bool isFishingMinigame = Game1.currentMinigame is FishingGame;
        bool hasBlockingMenu = Game1.activeClickableMenu is not null
            && Game1.activeClickableMenu is not BobberBar;
        if (!AutomationHudVisibilityPolicy.ShouldDraw(new(
                Game1.displayHUD,
                hasBlockingMenu,
                Game1.eventUp,
                Game1.isFestival(),
                Game1.currentMinigame is not null && !isFishingMinigame)))
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
            IsFishingMinigame: isFishingMinigame,
            IsFestival: Game1.isFestival(),
            IsToolbarAtTop: IsToolbarAtTop()));
        float opacity = toolbar is null ? 1f : Math.Clamp(toolbar.transparency, 0.33f, 1f);
        AutomationHudVisual visual = AutomationHudVisualPolicy.GetVisual(
            session.IsEnabled,
            session.State,
            session.LastReason);

        DrawRod(batch, AutomationHudLayout.PlaceIcon(bounds), opacity);

        if (visual.Badge != AutomationHudBadge.None)
            DrawBadge(batch, AutomationHudLayout.PlaceBadge(bounds), visual, opacity);

        if (TreasureTargetingHudVisualPolicy.ShouldDraw(config.TreasureTargeting))
            DrawTreasureTargeting(batch, AutomationHudLayout.PlaceTreasureIcon(bounds), opacity);

        return;

        bool IsToolbarAtTop()
        {
            if (Game1.options.pinToolbarToggle)
                return false;

            Vector2 playerPosition = Game1.GlobalToLocal(Game1.viewport, Game1.player.StandingPixel.ToVector2());
            return playerPosition.Y > Game1.uiViewport.Height / 2f + 64f;
        }
    }

    private static void DrawRod(SpriteBatch batch, Rectangle bounds, float opacity)
    {
        string rodId = Game1.player.CurrentTool is StardewValley.Tools.FishingRod rod
            ? rod.QualifiedItemId
            : FallbackRodId;
        ParsedItemData rodData = ItemRegistry.GetDataOrErrorItem(rodId);
        Texture2D texture = rodData.GetTexture();
        Rectangle source = rodData.GetSourceRect();
        Rectangle shadowBounds = new(
            bounds.X + AutomationHudLayout.IconShadowOffset,
            bounds.Y + AutomationHudLayout.IconShadowOffset,
            bounds.Width,
            bounds.Height);

        batch.Draw(
            texture,
            shadowBounds,
            source,
            Color.Black * (0.55f * opacity),
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            1f);
        batch.Draw(
            texture,
            bounds,
            source,
            Color.White * opacity,
            0f,
            Vector2.Zero,
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

        int baseEmoteIndex = visual.Badge switch
        {
            AutomationHudBadge.Disabled => Character.xEmote,
            AutomationHudBadge.Paused => Character.pauseEmote,
            AutomationHudBadge.LateNight => Character.sleepEmote,
            AutomationHudBadge.LowEnergy => Character.sadEmote,
            AutomationHudBadge.Warning => Character.exclamationEmote,
            AutomationHudBadge.Recovered => Character.happyEmote,
            AutomationHudBadge.Working => Character.musicNoteEmote,
            _ => -1
        };
        if (baseEmoteIndex < 0)
            return;

        DrawEmote(batch, bounds, baseEmoteIndex, opacity);
    }

    private static void DrawTreasureTargeting(
        SpriteBatch batch,
        Rectangle iconBounds,
        float opacity)
    {
        Rectangle shadowBounds = new(
            iconBounds.X + 2,
            iconBounds.Y + 2,
            iconBounds.Width,
            iconBounds.Height);
        batch.Draw(
            Game1.mouseCursors,
            shadowBounds,
            TreasureHunterSource,
            Color.Black * (0.55f * opacity),
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            1f);
        batch.Draw(
            Game1.mouseCursors,
            iconBounds,
            TreasureHunterSource,
            Color.White * opacity,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            1f);

    }

    private static void DrawEmote(SpriteBatch batch, Rectangle bounds, int baseEmoteIndex, float opacity)
    {
        const int sourceSize = 16;
        int emoteIndex = AutomationHudAnimation.GetEmoteFrame(
            baseEmoteIndex,
            Game1.currentGameTime.TotalGameTime.TotalMilliseconds);
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
