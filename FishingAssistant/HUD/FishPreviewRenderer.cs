using FishingAssistant.Configuration;
using FishingAssistant.Fishing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.HUD;

internal sealed class FishPreviewRenderer
{
    private static readonly Rectangle SonarBubbleSource = new(227, 6, 29, 24);

    private const int IconSize = 64;
    private const int PanelPadding = 16;
    private const int MaximumPanelWidth = 280;
    private const int SonarWidth = 116;
    private const int SonarHeight = 96;

    private readonly PerScreen<PreviewCache> caches = new(() => new PreviewCache());
    private readonly PerScreen<bool> fallbackWarnings = new(() => false);
    private readonly IMonitor monitor;

    public FishPreviewRenderer(IMonitor monitor)
    {
        this.monitor = monitor;
    }

    public void Draw(SpriteBatch batch, ModConfig config)
    {
        BobberBarAdapter? bar = BobberBarAdapter.ForCurrentScreen();
        if (bar is null)
            return;

        // BobberBar stores screen-local bounds. In split-screen, refresh them in
        // the active viewport before using them as the preview anchor.
        bar.RepositionForCurrentScreen();
        FishPreviewSnapshot snapshot = bar.ReadPreviewSnapshot();
        FishPreviewDecision decision = FishPreviewPolicy.Decide(new FishPreviewConditions(
            config.DisplayFishPreview,
            snapshot.IsReady,
            snapshot.WasCaught,
            snapshot.IsLegendary,
            config.ShowUncaughtFish,
            config.ShowLegendaryFish,
            config.ShowFishName,
            config.ShowTreasure,
            snapshot.HasTreasure,
            snapshot.IsGoldenTreasure));
        if (!decision.ShouldDraw)
            return;

        FishPreviewStyleDecision style = FishPreviewStylePolicy.Decide(new FishPreviewStyleConditions(
            config.FishPreviewStyle,
            SonarPreviewPatch.CanSuppressCurrentDraw,
            snapshot.HasSonarBobber));
        if (!style.ShouldDrawModPreview)
        {
            this.WarnForFallback();
            return;
        }
        if (style.UsedCompatibilityFallback)
            this.WarnForFallback();

        PreviewCache cache = this.caches.Value;
        if (!string.Equals(cache.ItemId, snapshot.ItemId, StringComparison.Ordinal))
        {
            cache.ItemId = snapshot.ItemId;
            cache.Item = ItemRegistry.Create(snapshot.ItemId);
        }

        bool drawClassic = style.EffectiveStyle == FishPreviewStyle.Classic;
        string label = decision.RevealFish ? snapshot.DisplayName : "???";
        int desiredWidth = drawClassic && decision.ShowFishName
            ? Math.Clamp((int)Math.Ceiling(Game1.smallFont.MeasureString(label).X) + PanelPadding * 2,
                FishPreviewLayout.MinimumWidth, MaximumPanelWidth)
            : drawClassic ? FishPreviewLayout.MinimumWidth : SonarWidth;
        int textWidth = Math.Max(1, desiredWidth - PanelPadding * 2);
        string wrappedLabel = drawClassic && decision.ShowFishName
            ? Game1.parseText(label, Game1.smallFont, textWidth)
            : string.Empty;
        int textHeight = drawClassic && decision.ShowFishName
            ? (int)Math.Ceiling(Game1.smallFont.MeasureString(wrappedLabel).Y) + 4
            : 0;
        int desiredHeight = drawClassic
            ? PanelPadding * 2 + IconSize + textHeight
            : SonarHeight;
        Rectangle bounds = FishPreviewLayout.Place(
            new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height),
            snapshot.BobberBounds,
            new Point(desiredWidth, desiredHeight));

        // BobberBar is drawn outside UI mode so it scales with the world zoom and
        // the active split-screen viewport. Draw the preview in that same coordinate
        // space instead of converting only its anchor to UI coordinates.
        Game1.StartWorldDrawInUI(batch);
        try
        {
            if (style.EffectiveStyle == FishPreviewStyle.Sonar)
            {
                DrawSonarPreview(batch, bounds, snapshot.BobberBounds, cache.Item!, decision);
                return;
            }

            DrawClassicPreview(batch, bounds, cache.Item!, decision, wrappedLabel);
        }
        finally
        {
            Game1.EndWorldDrawInUI(batch);
        }
    }

    private static void DrawClassicPreview(
        SpriteBatch batch,
        Rectangle bounds,
        Item item,
        FishPreviewDecision decision,
        string wrappedLabel)
    {
        IClickableMenu.drawTextureBox(
            batch,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            Color.White * 0.94f,
            drawShadow: true);

        Vector2 iconPosition = new(
            bounds.Center.X - IconSize / 2,
            bounds.Y + PanelPadding);
        Color fishColor = decision.RevealFish ? Color.White : Color.Black * 0.72f;
        item.drawInMenu(
            batch,
            iconPosition,
            1f,
            1f,
            0.9f,
            StackDrawType.Hide,
            fishColor,
            drawShadow: decision.RevealFish);

        if (decision.ShowTreasure)
        {
            Texture2D treasureTexture = decision.IsGoldenTreasure
                ? Game1.mouseCursors_1_6
                : Game1.mouseCursors;
            Rectangle treasureSource = decision.IsGoldenTreasure
                ? new Rectangle(256, 51, 20, 24)
                : new Rectangle(638, 1865, 20, 24);
            batch.Draw(
                treasureTexture,
                iconPosition + new Vector2(54f, 52f),
                treasureSource,
                Color.White,
                0f,
                new Vector2(10f, 12f),
                1.2f,
                SpriteEffects.None,
                0.91f);
        }

        if (decision.ShowFishName)
        {
            Vector2 textSize = Game1.smallFont.MeasureString(wrappedLabel);
            Vector2 textPosition = new(
                bounds.Center.X - textSize.X / 2f,
                iconPosition.Y + IconSize + 4f);
            Utility.drawTextWithShadow(batch, wrappedLabel, Game1.smallFont, textPosition, Game1.textColor);
        }
    }

    private static void DrawSonarPreview(
        SpriteBatch batch,
        Rectangle bounds,
        Rectangle anchor,
        Item item,
        FishPreviewDecision decision)
    {
        bool flipHorizontally = bounds.Center.X < anchor.Center.X;
        DrawSonarBubble(batch, bounds, flipHorizontally);

        // Match Vanilla's asymmetric placement so the fish remains clear of the
        // speech-bubble pointer on either side.
        Vector2 iconPosition = new(
            bounds.X + (flipHorizontally ? 20f : 36f),
            bounds.Y + 16f);
        Color fishColor = decision.RevealFish ? Color.White : Color.Black * 0.72f;
        item.drawInMenu(batch, iconPosition, 1f, 1f, 0.9f, StackDrawType.Hide,
            fishColor, drawShadow: decision.RevealFish);

        if (decision.ShowTreasure)
        {
            Texture2D treasureTexture = decision.IsGoldenTreasure
                ? Game1.mouseCursors_1_6
                : Game1.mouseCursors;
            Rectangle treasureSource = decision.IsGoldenTreasure
                ? new Rectangle(256, 51, 20, 24)
                : new Rectangle(638, 1865, 20, 24);
            batch.Draw(treasureTexture, iconPosition + new Vector2(54f, 52f), treasureSource,
                Color.White, 0f, new Vector2(10f, 12f), 1.2f,
                SpriteEffects.None, 0.92f);
        }

    }

    private static void DrawSonarBubble(SpriteBatch batch, Rectangle bounds, bool flipHorizontally)
    {
        batch.Draw(
            Game1.mouseCursors_1_6,
            bounds,
            SonarBubbleSource,
            Color.White,
            0f,
            Vector2.Zero,
            flipHorizontally ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            0.88f);
    }

    private void WarnForFallback()
    {
        if (this.fallbackWarnings.Value)
            return;

        this.fallbackWarnings.Value = true;
        this.monitor.Log(
            "Vanilla Sonar preview suppression wasn't available for the current draw; " +
            "Fishing Assistant used its compatible preview fallback.",
            LogLevel.Warn);
    }

    private sealed class PreviewCache
    {
        public string? ItemId { get; set; }

        public Item? Item { get; set; }
    }
}
