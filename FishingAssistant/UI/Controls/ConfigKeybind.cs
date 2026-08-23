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
    private readonly Func<KeybindList>? getOptionalValue;
    private readonly Action<KeybindList>? setOptionalValue;
    private readonly string listeningText;
    private KeybindCaptureGate? captureGate;
    private bool isListeningForOptional;

    public ConfigKeybind(
        int id,
        Rectangle bounds,
        string label,
        string description,
        string listeningText,
        Func<KeybindList> getValue,
        Action<KeybindList> setValue,
        Func<KeybindList>? getOptionalValue = null,
        Action<KeybindList>? setOptionalValue = null)
    {
        this.Component = new ClickableComponent(bounds, label) { myID = id };
        this.Description = description;
        this.listeningText = listeningText;
        this.getValue = getValue;
        this.setValue = setValue;
        this.getOptionalValue = getOptionalValue;
        this.setOptionalValue = setOptionalValue;
    }

    public ClickableComponent Component { get; }

    public string Description { get; }

    public int InlineMessageRight
    {
        get
        {
            Rectangle bounds = this.Component.bounds;
            return bounds.Right - MenuVisualMetrics.GetControlWidth(bounds.Width) - 8;
        }
    }

    public bool IsListening { get; private set; }

    public void ReceiveLeftClick(int x, int y)
    {
        if (this.IsListening)
            return;

        this.isListeningForOptional = this.getOptionalValue is not null
            && (this.GetOptionalValueBounds().Contains(x, y)
                || Game1.options.gamepadControls);
        this.IsListening = true;
        this.captureGate = new KeybindCaptureGate();
        GameMenu.forcePreventClose = true;
        Game1.playSound("breathin");
    }

    public bool Adjust(int direction) => false;

    public IReadOnlyList<SButton> ObserveInput(
        IReadOnlyList<SButton> pressed,
        IReadOnlyList<SButton> held)
    {
        if (!this.IsListening || this.captureGate is null)
            return [];

        IReadOnlyList<SButton> buttons = this.captureGate.Observe(pressed, held);
        if (buttons.Count == 0)
            return [];

        KeybindCaptureResult result = KeybindCapture.Resolve(buttons);
        if (result.Action == KeybindCaptureAction.Cancel)
        {
            this.StopListening();
            Game1.playSound("bigDeSelect");
            return buttons;
        }

        if (result.Action == KeybindCaptureAction.Clear)
        {
            this.SetListeningValue(new KeybindList(SButton.None));
        }
        else
        {
            KeybindList binding = KeybindList.ForSingle([.. result.Buttons]);
            this.SetListeningValue(binding);
        }

        this.StopListening();
        Game1.playSound("coin");
        return buttons;
    }

    public void CancelListening()
    {
        if (this.IsListening)
            this.StopListening();
    }

    public void Draw(SpriteBatch batch, bool highlighted, int labelBottomInset = 0)
    {
        Rectangle bounds = this.Component.bounds;
        if (highlighted)
            batch.Draw(Game1.staminaRect, bounds, Color.Wheat * 0.28f);

        int valueWidth = MenuVisualMetrics.GetControlWidth(bounds.Width);
        int valueHeight = MenuVisualMetrics.GetControlHeight(bounds.Height);
        string label = MenuText.Fit(this.Component.name, Game1.smallFont, bounds.Width - valueWidth - 20);
        Vector2 labelPosition = new(bounds.X + 8,
            bounds.Center.Y - Game1.smallFont.LineSpacing / 2f - labelBottomInset / 2f);
        Utility.drawTextWithShadow(batch, label, Game1.smallFont, labelPosition, Game1.textColor);

        if (this.getOptionalValue is null)
        {
            Rectangle valueBounds = new(bounds.Right - valueWidth, bounds.Center.Y - valueHeight / 2,
                valueWidth, valueHeight);
            this.DrawValueButton(batch, valueBounds,
                this.IsListening ? this.listeningText : FormatBinding(this.getValue()),
                this.IsListening);
            return;
        }

        Rectangle mainBounds = this.GetMainValueBounds();
        Rectangle optionalBounds = this.GetOptionalValueBounds();
        this.DrawValueButton(batch, mainBounds,
            this.IsListening && !this.isListeningForOptional
                ? this.listeningText
                : FormatBinding(this.getValue()),
            this.IsListening && !this.isListeningForOptional);
        this.DrawValueButton(batch, optionalBounds,
            this.IsListening && this.isListeningForOptional
                ? this.listeningText
                : FormatBinding(this.getOptionalValue()),
            this.IsListening && this.isListeningForOptional);
    }

    private void StopListening()
    {
        this.IsListening = false;
        this.isListeningForOptional = false;
        this.captureGate = null;
        GameMenu.forcePreventClose = false;
    }

    private void SetListeningValue(KeybindList value)
    {
        if (this.isListeningForOptional && this.setOptionalValue is not null)
            this.setOptionalValue(value);
        else
            this.setValue(value);
    }

    private Rectangle GetMainValueBounds()
    {
        Rectangle bounds = this.Component.bounds;
        int totalWidth = MenuVisualMetrics.GetControlWidth(bounds.Width);
        int gap = 8;
        int buttonWidth = (totalWidth - gap) / 2;
        int height = MenuVisualMetrics.GetControlHeight(bounds.Height);
        return new Rectangle(
            bounds.Right - totalWidth,
            bounds.Center.Y - height / 2,
            buttonWidth,
            height);
    }

    private Rectangle GetOptionalValueBounds()
    {
        Rectangle main = this.GetMainValueBounds();
        return new Rectangle(main.Right + 8, main.Y, main.Width, main.Height);
    }

    private void DrawValueButton(SpriteBatch batch, Rectangle bounds, string value, bool listening)
    {
        string fittedValue = MenuText.Fit(value, Game1.smallFont, bounds.Width - 20);
        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            bounds.X, bounds.Y, bounds.Width, bounds.Height,
            listening ? Color.Wheat : Color.White);
        Vector2 size = Game1.smallFont.MeasureString(fittedValue);
        Utility.drawTextWithShadow(batch, fittedValue, Game1.smallFont,
            new Vector2(bounds.Center.X - size.X / 2f, bounds.Center.Y - size.Y / 2f),
            Game1.textColor);
    }

    private static string FormatBinding(KeybindList keybinds)
    {
        if (!keybinds.IsBound)
            return SButton.None.ToString();

        return string.Join(", ", keybinds.Keybinds.Select(keybind =>
            string.Join(" + ", keybind.Buttons.Select(button =>
            {
                string label = button.ToString();
                return button.TryGetController(out _)
                    ? label["Controller".Length..]
                    : label;
            }))));
    }
}
