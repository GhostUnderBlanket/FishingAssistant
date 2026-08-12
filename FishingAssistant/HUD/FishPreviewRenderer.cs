using FishingAssistant.Configuration;
using FishingAssistant.Fishing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.HUD;

internal sealed class FishPreviewRenderer
{
    private const int IconSize = 64;
    private const int PanelPadding = 16;
    private const int MaximumPanelWidth = 280;

    private readonly PerScreen<PreviewCache> caches = new(() => new PreviewCache());

    public void Draw(SpriteBatch batch, ModConfig config)
    {
        BobberBarAdapter? bar = BobberBarAdapter.ForCurrentScreen();
        if (bar is null)
            return;

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

        PreviewCache cache = this.caches.Value;
        if (!string.Equals(cache.ItemId, snapshot.ItemId, StringComparison.Ordinal))
        {
            cache.ItemId = snapshot.ItemId;
            cache.Item = ItemRegistry.Create(snapshot.ItemId);
        }

        string label = decision.RevealFish ? snapshot.DisplayName : "???";
        int desiredWidth = decision.ShowFishName
            ? Math.Clamp((int)Math.Ceiling(Game1.smallFont.MeasureString(label).X) + PanelPadding * 2,
                FishPreviewLayout.MinimumWidth, MaximumPanelWidth)
            : FishPreviewLayout.MinimumWidth;
        int textWidth = Math.Max(1, desiredWidth - PanelPadding * 2);
        string wrappedLabel = decision.ShowFishName
            ? Game1.parseText(label, Game1.smallFont, textWidth)
            : string.Empty;
        int textHeight = decision.ShowFishName
            ? (int)Math.Ceiling(Game1.smallFont.MeasureString(wrappedLabel).Y) + 4
            : 0;
        int desiredHeight = PanelPadding * 2 + IconSize + textHeight;
        Rectangle viewport = new(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height);
        Rectangle bounds = FishPreviewLayout.Place(
            viewport,
            snapshot.BobberBounds,
            new Point(desiredWidth, desiredHeight));

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
        cache.Item!.drawInMenu(
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

    private sealed class PreviewCache
    {
        public string? ItemId { get; set; }

        public Item? Item { get; set; }
    }
}
