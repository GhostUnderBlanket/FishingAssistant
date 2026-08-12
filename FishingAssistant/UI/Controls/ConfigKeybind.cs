using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.UI.Controls;

internal sealed class ConfigKeybind : IConfigControl
{
    private readonly Func<KeybindList> getValue;
    private readonly Action<KeybindList> setValue;
    private readonly string listeningText;

    public ConfigKeybind(
        int id,
        Rectangle bounds,
        string label,
        string description,
        string listeningText,
        Func<KeybindList> getValue,
        Action<KeybindList> setValue)
    {
        this.Component = new ClickableComponent(bounds, label) { myID = id };
        this.Description = description;
        this.listeningText = listeningText;
        this.getValue = getValue;
        this.setValue = setValue;
    }

    public ClickableComponent Component { get; }

    public string Description { get; }

    public bool IsListening { get; private set; }

    public void ReceiveLeftClick(int x, int y)
    {
        if (this.IsListening)
            return;

        this.IsListening = true;
        GameMenu.forcePreventClose = true;
        Game1.playSound("breathin");
    }

    public bool Adjust(int direction) => false;

    public void Capture(IReadOnlyList<SButton> buttons)
    {
        if (!this.IsListening || buttons.Count == 0)
            return;

        KeybindCaptureResult result = KeybindCapture.Resolve(buttons);
        if (result.Action == KeybindCaptureAction.Cancel)
        {
            this.StopListening();
            Game1.playSound("bigDeSelect");
            return;
        }

        if (result.Action == KeybindCaptureAction.Clear)
            this.setValue(new KeybindList(SButton.None));
        else
            this.setValue(KeybindList.ForSingle([.. result.Buttons]));

        this.StopListening();
        Game1.playSound("coin");
    }

    public void CancelListening()
    {
        if (this.IsListening)
            this.StopListening();
    }

    public void Draw(SpriteBatch batch, bool highlighted)
    {
        Rectangle bounds = this.Component.bounds;
        if (highlighted)
            batch.Draw(Game1.staminaRect, bounds, Color.Wheat * 0.28f);

        string value = this.IsListening ? this.listeningText : this.getValue().ToString();
        int valueWidth = MenuVisualMetrics.GetControlWidth(bounds.Width);
        int valueHeight = MenuVisualMetrics.GetControlHeight(bounds.Height);
        string label = MenuText.Fit(this.Component.name, Game1.smallFont, bounds.Width - valueWidth - 20);
        string fittedValue = MenuText.Fit(value, Game1.smallFont, valueWidth - 20);
        Vector2 labelPosition = new(bounds.X + 8, bounds.Center.Y - Game1.smallFont.LineSpacing / 2f);
        Utility.drawTextWithShadow(batch, label, Game1.smallFont, labelPosition, Game1.textColor);

        Rectangle valueBounds = new(bounds.Right - valueWidth, bounds.Center.Y - valueHeight / 2,
            valueWidth, valueHeight);
        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            valueBounds.X, valueBounds.Y, valueBounds.Width, valueBounds.Height,
            this.IsListening ? Color.Wheat : Color.White);
        Vector2 size = Game1.smallFont.MeasureString(fittedValue);
        Utility.drawTextWithShadow(batch, fittedValue, Game1.smallFont,
            new Vector2(valueBounds.Center.X - size.X / 2f, valueBounds.Center.Y - size.Y / 2f),
            Game1.textColor);
    }

    private void StopListening()
    {
        this.IsListening = false;
        GameMenu.forcePreventClose = false;
    }
}
