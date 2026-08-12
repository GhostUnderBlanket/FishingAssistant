using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.UI.Controls;

internal sealed class ConfigActionButton : IConfigControl
{
    private readonly Func<string> getButtonLabel;
    private readonly Action activate;

    public ConfigActionButton(
        int id,
        Rectangle bounds,
        string label,
        string description,
        Func<string> getButtonLabel,
        Action activate)
    {
        this.Component = new ClickableComponent(bounds, label) { myID = id };
        this.Description = description;
        this.getButtonLabel = getButtonLabel;
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

        int buttonWidth = Math.Clamp(bounds.Width * 2 / 5, 112, 260);
        Rectangle buttonBounds = new(
            bounds.Right - buttonWidth,
            bounds.Center.Y - Math.Min(44, Math.Max(1, bounds.Height - 8)) / 2,
            buttonWidth,
            Math.Min(44, Math.Max(1, bounds.Height - 8))
        );
        string label = MenuText.Fit(this.Component.name, Game1.smallFont,
            buttonBounds.Left - bounds.Left - 20);
        Utility.drawTextWithShadow(batch, label, Game1.smallFont,
            new Vector2(bounds.X + 8, bounds.Center.Y - Game1.smallFont.LineSpacing / 2f), Game1.textColor);

        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            buttonBounds.X, buttonBounds.Y, buttonBounds.Width, buttonBounds.Height,
            highlighted ? Color.Wheat : Color.White);
        string buttonLabel = MenuText.Fit(this.getButtonLabel(), Game1.smallFont, buttonBounds.Width - 16);
        Vector2 size = Game1.smallFont.MeasureString(buttonLabel);
        Utility.drawTextWithShadow(batch, buttonLabel, Game1.smallFont,
            new Vector2(buttonBounds.Center.X - size.X / 2f, buttonBounds.Center.Y - size.Y / 2f),
            Game1.textColor);
    }
}
