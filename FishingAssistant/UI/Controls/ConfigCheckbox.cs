using FishingAssistant.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.UI.Controls;

internal sealed class ConfigCheckbox
{
    private readonly Func<bool> getValue;
    private readonly Action<bool> setValue;

    public ConfigCheckbox(
        int id,
        Rectangle bounds,
        string label,
        string description,
        Func<bool> getValue,
        Action<bool> setValue)
    {
        this.Component = new ClickableComponent(bounds, label)
        {
            myID = id
        };
        this.Description = description;
        this.getValue = getValue;
        this.setValue = setValue;
    }

    public ClickableComponent Component { get; }

    public string Description { get; }

    public void Toggle()
    {
        bool value = !this.getValue();
        this.setValue(value);
        Game1.playSound(value ? "drumkit6" : "breathin");
    }

    public void Draw(SpriteBatch batch, bool highlighted)
    {
        Rectangle bounds = this.Component.bounds;
        if (highlighted)
            batch.Draw(Game1.staminaRect, bounds, Color.Wheat * 0.28f);

        bool value = this.getValue();
        int checkboxSize = 9 * Game1.pixelZoom;
        Vector2 checkboxPosition = new(
            bounds.Right - checkboxSize,
            bounds.Center.Y - checkboxSize / 2
        );
        Rectangle source = value ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked;
        batch.Draw(Game1.mouseCursors, checkboxPosition, source, Color.White, 0f, Vector2.Zero,
            Game1.pixelZoom, SpriteEffects.None, 0.4f);

        Vector2 labelPosition = new(bounds.X + 8, bounds.Center.Y - Game1.smallFont.LineSpacing / 2f);
        string label = MenuText.Fit(this.Component.name, Game1.smallFont,
            bounds.Width - checkboxSize - 24);
        Utility.drawTextWithShadow(batch, label, Game1.smallFont, labelPosition, Game1.textColor);
    }
}
