using FishingAssistant.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace FishingAssistant.UI.Controls;

internal sealed class ConfigItemPicker : IConfigControl
{
    private readonly Func<string> getValue;
    private readonly Func<string, string> getLabel;
    private readonly Action activate;

    public ConfigItemPicker(
        int id,
        Rectangle bounds,
        string label,
        string description,
        Func<string> getValue,
        Func<string, string> getLabel,
        Action activate)
    {
        this.Component = new ClickableComponent(bounds, label) { myID = id };
        this.Description = description;
        this.getValue = getValue;
        this.getLabel = getLabel;
        this.activate = activate;
    }

    public ClickableComponent Component { get; }

    public string Description { get; }

    public void ReceiveLeftClick(int x, int y)
    {
        this.activate();
        Game1.playSound("bigSelect");
    }

    public bool Adjust(int direction) => false;

    public void Draw(SpriteBatch batch, bool highlighted)
    {
        Rectangle bounds = this.Component.bounds;
        if (highlighted)
            batch.Draw(Game1.staminaRect, bounds, Color.Wheat * 0.28f);

        int pickerWidth = MenuVisualMetrics.GetControlWidth(bounds.Width);
        int pickerHeight = MenuVisualMetrics.GetControlHeight(bounds.Height);
        Rectangle pickerBounds = new(bounds.Right - pickerWidth, bounds.Center.Y - pickerHeight / 2,
            pickerWidth, pickerHeight);
        string label = MenuText.Fit(this.Component.name, Game1.smallFont,
            pickerBounds.Left - bounds.Left - 20);
        Utility.drawTextWithShadow(batch, label, Game1.smallFont,
            new Vector2(bounds.X + 8, bounds.Center.Y - Game1.smallFont.LineSpacing / 2f), Game1.textColor);

        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            pickerBounds.X, pickerBounds.Y, pickerBounds.Width, pickerBounds.Height,
            highlighted ? Color.Wheat : Color.White);

        string value = this.getValue();
        int textLeft = pickerBounds.X + 18;
        ParsedItemData? data = value.StartsWith('(') ? ItemRegistry.GetData(value) : null;
        if (data is not null)
        {
            Rectangle source = data.GetSourceRect();
            float scale = Math.Min(2.25f, (pickerBounds.Height - 12f) / source.Height);
            batch.Draw(data.GetTexture(), new Vector2(pickerBounds.X + 12,
                    pickerBounds.Center.Y - source.Height * scale / 2f),
                source, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);
            textLeft = pickerBounds.X + 58;
        }

        string display = MenuText.Fit(this.getLabel(value), Game1.smallFont,
            pickerBounds.Right - textLeft - 22);
        Vector2 size = Game1.smallFont.MeasureString(display);
        Utility.drawTextWithShadow(batch, display, Game1.smallFont,
            new Vector2(textLeft, pickerBounds.Center.Y - size.Y / 2f), Game1.textColor);
    }
}
