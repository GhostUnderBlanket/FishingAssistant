using FishingAssistant.Configuration;
using FishingAssistant.Fishing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace FishingAssistant.HUD;

internal sealed class FishingBubbleMarkerRenderer(Func<BubbleCastPlan?> getPlan)
{
    private Texture2D? markerMask;
    private Texture2D? markerSource;

    public void Draw(SpriteBatch batch, ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(config);

        if (!config.ShowFishingBubbleMarker
            || !Context.IsWorldReady
            || Game1.activeClickableMenu is not null
            || Game1.currentMinigame is not null)
            return;

        BubbleCastPlan? plan = getPlan();
        if (plan is null
            || plan.BubbleTile == Point.Zero
            || !Utility.isOnScreen(plan.BubbleTile, Game1.tileSize, Game1.currentLocation))
            return;

        Vector2 position = Game1.GlobalToLocal(
            Game1.viewport,
            new Vector2(plan.BubbleTile.X * Game1.tileSize, plan.BubbleTile.Y * Game1.tileSize));
        Texture2D mask = this.GetMarkerMask();
        batch.Draw(
            mask,
            position,
            sourceRectangle: null,
            plan.IsReachable ? Color.LimeGreen : Color.Gold,
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            0.01f);
    }

    private Texture2D GetMarkerMask()
    {
        Texture2D source = Game1.mouseCursors;
        if (this.markerMask is not null
            && !this.markerMask.IsDisposed
            && ReferenceEquals(this.markerSource, source))
            return this.markerMask;

        this.markerMask?.Dispose();

        Rectangle sourceRectangle = Game1.getSourceRectForStandardTileSheet(source, 29);
        Color[] pixels = new Color[sourceRectangle.Width * sourceRectangle.Height];
        source.GetData(0, sourceRectangle, pixels, 0, pixels.Length);
        for (int index = 0; index < pixels.Length; index++)
        {
            byte alpha = pixels[index].A;
            pixels[index] = Color.FromNonPremultiplied(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);
        }

        this.markerMask = new Texture2D(
            Game1.graphics.GraphicsDevice,
            sourceRectangle.Width,
            sourceRectangle.Height);
        this.markerMask.SetData(pixels);
        this.markerSource = source;
        return this.markerMask;
    }
}
