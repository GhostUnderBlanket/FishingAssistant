using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.UI.Controls;

internal sealed class ConfigValueSelector<T> : IConfigControl
{
    private static readonly Rectangle ArrowSource = new(421, 459, 11, 12);
    private readonly Func<T> getValue;
    private readonly Action<T> setValue;
    private readonly Func<T, int, T> adjustValue;
    private readonly Func<T, string> formatValue;

    public ConfigValueSelector(
        int id,
        Rectangle bounds,
        string label,
        string description,
        Func<T> getValue,
        Action<T> setValue,
        Func<T, int, T> adjustValue,
        Func<T, string> formatValue)
    {
        this.Component = new ClickableComponent(bounds, label) { myID = id };
        this.Description = description;
        this.getValue = getValue;
        this.setValue = setValue;
        this.adjustValue = adjustValue;
        this.formatValue = formatValue;
    }

    public ClickableComponent Component { get; }

    public string Description { get; }

    public void ReceiveLeftClick(int x, int y)
    {
        Rectangle valueBounds = this.GetValueBounds();
        int direction = x >= valueBounds.Left && x < valueBounds.Center.X ? -1 : 1;
        this.Adjust(direction);
    }

    public bool Adjust(int direction)
    {
        T current = this.getValue();
        T adjusted = this.adjustValue(current, direction);
        if (EqualityComparer<T>.Default.Equals(current, adjusted))
            return true;

        this.setValue(adjusted);
        Game1.playSound("shwip");
        return true;
    }

    public void Draw(SpriteBatch batch, bool highlighted)
    {
        Rectangle bounds = this.Component.bounds;
        if (highlighted)
            batch.Draw(Game1.staminaRect, bounds, Color.Wheat * 0.28f);

        Rectangle valueBounds = this.GetValueBounds();
        string label = MenuText.Fit(this.Component.name, Game1.smallFont,
            valueBounds.Left - bounds.Left - 20);
        Vector2 labelPosition = new(bounds.X + 8, bounds.Center.Y - Game1.smallFont.LineSpacing / 2f);
        Utility.drawTextWithShadow(batch, label, Game1.smallFont, labelPosition, Game1.textColor);

        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            valueBounds.X, valueBounds.Y, valueBounds.Width, valueBounds.Height, Color.White);
        this.DrawArrow(batch, new Vector2(valueBounds.Left + 18, valueBounds.Center.Y), -MathF.PI / 2f);
        this.DrawArrow(batch, new Vector2(valueBounds.Right - 18, valueBounds.Center.Y), MathF.PI / 2f);

        string value = MenuText.Fit(this.formatValue(this.getValue()), Game1.smallFont, valueBounds.Width - 72);
        Vector2 size = Game1.smallFont.MeasureString(value);
        Utility.drawTextWithShadow(batch, value, Game1.smallFont,
            new Vector2(valueBounds.Center.X - size.X / 2f, valueBounds.Center.Y - size.Y / 2f),
            Game1.textColor);
    }

    private Rectangle GetValueBounds()
    {
        Rectangle bounds = this.Component.bounds;
        int width = Math.Clamp(bounds.Width * 2 / 5, 112, 260);
        int height = Math.Min(44, Math.Max(1, bounds.Height - 8));
        return new Rectangle(bounds.Right - width, bounds.Center.Y - height / 2, width, height);
    }

    private void DrawArrow(SpriteBatch batch, Vector2 position, float rotation)
    {
        batch.Draw(Game1.mouseCursors, position, ArrowSource, Color.White, rotation,
            new Vector2(ArrowSource.Width / 2f, ArrowSource.Height / 2f), 2f,
            SpriteEffects.None, 0.9f);
    }
}
