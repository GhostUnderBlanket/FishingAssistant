using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.UI.Controls;

internal sealed class ConfigSlider : IConfigControl, IKeyboardSubscriber
{
    private const int DragThreshold = 4;
    private const int KnobMinimumWidth = 72;

    private readonly Func<double> getValue;
    private readonly Action<double> setValue;
    private readonly Func<double, string> formatValue;
    private readonly double minimum;
    private readonly double maximum;
    private readonly double increment;
    private readonly int knobMinimumWidth;
    private Point pointerStart;
    private bool pointerActive;
    private bool pointerStartedOnKnob;
    private bool pointerDragged;
    private bool pointerValueChanged;
    private string editText = "";

    public ConfigSlider(
        int id,
        Rectangle bounds,
        string label,
        string description,
        Func<double> getValue,
        Action<double> setValue,
        double minimum,
        double maximum,
        double increment,
        Func<double, string> formatValue,
        int knobMinimumWidth = KnobMinimumWidth)
    {
        if (maximum <= minimum)
            throw new ArgumentOutOfRangeException(nameof(maximum));
        if (increment <= 0)
            throw new ArgumentOutOfRangeException(nameof(increment));
        if (knobMinimumWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(knobMinimumWidth));

        this.Component = new ClickableComponent(bounds, label) { myID = id };
        this.Description = description;
        this.getValue = getValue;
        this.setValue = setValue;
        this.minimum = minimum;
        this.maximum = maximum;
        this.increment = increment;
        this.formatValue = formatValue;
        this.knobMinimumWidth = knobMinimumWidth;
    }

    public ClickableComponent Component { get; }

    public string Description { get; }

    public int InlineMessageRight => this.GetTrackBounds().Left - 8;

    public bool IsEditing { get; private set; }

    public bool IsPointerActive => this.pointerActive;

    public bool Selected
    {
        get => this.IsEditing;
        set
        {
            if (!value)
                this.CancelEditing();
        }
    }

    public void ReceiveLeftClick(int x, int y)
    {
        Rectangle track = this.GetTrackBounds();
        if (!track.Contains(x, y))
            return;

        if (this.IsEditing)
            this.CommitEditing();

        this.pointerStart = new Point(x, y);
        this.pointerActive = true;
        this.pointerDragged = false;
        this.pointerValueChanged = false;
        this.pointerStartedOnKnob = this.GetKnobBounds(track).Contains(x, y);
        if (!this.pointerStartedOnKnob)
        {
            this.SetFromPointer(x);
            this.pointerDragged = true;
        }
    }

    public void LeftClickHeld(int x, int y)
    {
        if (!this.pointerActive)
            return;

        if (!this.pointerDragged
            && Math.Abs(x - this.pointerStart.X) < DragThreshold
            && Math.Abs(y - this.pointerStart.Y) < DragThreshold)
            return;

        this.pointerDragged = true;
        this.SetFromPointer(x);
    }

    public void ReleaseLeftClick(int x, int y)
    {
        if (!this.pointerActive)
            return;

        bool startEditing = this.pointerStartedOnKnob && !this.pointerDragged;
        bool playAdjustmentSound = this.pointerValueChanged;
        this.pointerActive = false;
        this.pointerStartedOnKnob = false;
        this.pointerDragged = false;
        this.pointerValueChanged = false;
        if (startEditing)
            this.BeginEditing();
        else if (playAdjustmentSound)
            Game1.playSound("shwip");
    }

    public bool Adjust(int direction)
    {
        if (this.IsEditing)
            return true;

        double current = this.getValue();
        double adjusted = this.Normalize(current + Math.Sign(direction) * this.increment);
        if (Math.Abs(current - adjusted) < 0.000001)
            return true;

        this.setValue(adjusted);
        Game1.playSound("shwip");
        return true;
    }

    public void BeginEditing()
    {
        if (this.IsEditing)
            return;

        this.editText = this.getValue().ToString("0.##", CultureInfo.InvariantCulture);
        this.IsEditing = true;
        Game1.keyboardDispatcher.Subscriber = this;
        GameMenu.forcePreventClose = true;
        Game1.playSound("breathin");
    }

    public void CommitEditing()
    {
        if (!this.IsEditing)
            return;

        string normalizedText = this.editText.Replace(',', '.');
        if (double.TryParse(normalizedText, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out double value))
        {
            this.setValue(this.Normalize(value));
            Game1.playSound("coin");
        }
        else
        {
            Game1.playSound("cancel");
        }

        this.StopEditing();
    }

    public void CancelEditing()
    {
        if (!this.IsEditing)
            return;

        this.StopEditing();
        Game1.playSound("bigDeSelect");
    }

    public void Draw(SpriteBatch batch, bool highlighted, int labelBottomInset = 0)
    {
        Rectangle bounds = this.Component.bounds;
        if (highlighted)
            batch.Draw(Game1.staminaRect, bounds, Color.Wheat * 0.28f);

        Rectangle track = this.GetTrackBounds();
        Rectangle knob = this.GetKnobBounds(track);
        string label = MenuText.Fit(this.Component.name, Game1.smallFont,
            track.Left - bounds.Left - 20);
        Vector2 labelPosition = new(bounds.X + 8,
            bounds.Center.Y - Game1.smallFont.LineSpacing / 2f - labelBottomInset / 2f);
        Utility.drawTextWithShadow(batch, label, Game1.smallFont, labelPosition, Game1.textColor);

        int trackY = track.Center.Y;
        batch.Draw(Game1.staminaRect,
            new Rectangle(track.X + 8, trackY - 3, Math.Max(1, track.Width - 16), 6),
            new Color(122, 66, 33));
        batch.Draw(Game1.staminaRect,
            new Rectangle(track.X + 8, trackY - 2, Math.Max(1, knob.Center.X - track.X - 8), 4),
            new Color(224, 144, 67));

        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            knob.X, knob.Y, knob.Width, knob.Height,
            this.IsEditing ? Color.Wheat : Color.White);

        string value = this.IsEditing ? this.editText : this.formatValue(this.getValue());
        value = MenuText.Fit(value, Game1.smallFont, knob.Width - 16);
        Vector2 size = Game1.smallFont.MeasureString(value);
        Utility.drawTextWithShadow(batch, value, Game1.smallFont,
            new Vector2(knob.Center.X - size.X / 2f, knob.Center.Y - size.Y / 2f),
            Game1.textColor);

        if (this.IsEditing
            && Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000d >= 500d)
        {
            int caretX = Math.Min(knob.Right - 8, knob.Center.X + (int)Math.Ceiling(size.X / 2f) + 2);
            batch.Draw(Game1.staminaRect,
                new Rectangle(caretX, knob.Center.Y - Game1.smallFont.LineSpacing / 2, 2,
                    Game1.smallFont.LineSpacing),
                Game1.textColor);
        }
    }

    public void RecieveTextInput(char inputChar)
    {
        if (!this.IsEditing)
            return;

        if (char.IsDigit(inputChar))
        {
            if (this.editText.Length < 8)
                this.editText += inputChar;
            return;
        }

        if ((inputChar is '.' or ',')
            && !this.editText.Contains('.')
            && !this.editText.Contains(','))
            this.editText += inputChar;
    }

    public void RecieveTextInput(string text)
    {
        foreach (char character in text)
            this.RecieveTextInput(character);
    }

    public void RecieveCommandInput(char command)
    {
        if (command == '\b' && this.editText.Length > 0)
            this.editText = this.editText[..^1];
    }

    public void RecieveSpecialInput(Keys key)
    {
        switch (key)
        {
            case Keys.Enter:
                this.CommitEditing();
                break;
            case Keys.Escape:
                this.CancelEditing();
                break;
        }
    }

    private void SetFromPointer(int x)
    {
        Rectangle track = this.GetTrackBounds();
        int knobWidth = this.GetKnobWidth();
        int left = track.Left + knobWidth / 2;
        int right = track.Right - knobWidth / 2;
        double ratio = right <= left
            ? 0d
            : Math.Clamp((x - left) / (double)(right - left), 0d, 1d);
        double current = this.getValue();
        double adjusted = this.Normalize(this.minimum + (this.maximum - this.minimum) * ratio);
        if (Math.Abs(current - adjusted) < 0.000001)
            return;

        this.setValue(adjusted);
        this.pointerValueChanged = true;
    }

    private double Normalize(double value)
    {
        double clamped = Math.Clamp(value, this.minimum, this.maximum);
        double steps = Math.Round((clamped - this.minimum) / this.increment,
            MidpointRounding.AwayFromZero);
        double snapped = this.minimum + steps * this.increment;
        return Math.Clamp(Math.Round(snapped, 6), this.minimum, this.maximum);
    }

    private Rectangle GetTrackBounds()
    {
        Rectangle bounds = this.Component.bounds;
        int width = MenuVisualMetrics.GetControlWidth(bounds.Width);
        int height = MenuVisualMetrics.GetControlHeight(bounds.Height);
        return new Rectangle(bounds.Right - width, bounds.Center.Y - height / 2, width, height);
    }

    private Rectangle GetKnobBounds(Rectangle track)
    {
        int width = this.GetKnobWidth();
        double ratio = (Math.Clamp(this.getValue(), this.minimum, this.maximum) - this.minimum)
            / (this.maximum - this.minimum);
        int centerX = track.Left + width / 2
            + (int)Math.Round((track.Width - width) * ratio);
        return new Rectangle(centerX - width / 2, track.Y, width, track.Height);
    }

    private int GetKnobWidth()
    {
        string widest = new[]
            {
                this.formatValue(this.minimum),
                this.formatValue(this.maximum),
                this.editText
            }
            .OrderByDescending(value => Game1.smallFont.MeasureString(value).X)
            .First();
        return Math.Clamp((int)Math.Ceiling(Game1.smallFont.MeasureString(widest).X) + 24,
            this.knobMinimumWidth,
            Math.Max(this.knobMinimumWidth,
                MenuVisualMetrics.GetControlWidth(this.Component.bounds.Width) / 2));
    }

    private void StopEditing()
    {
        this.IsEditing = false;
        this.editText = "";
        if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, this))
            Game1.keyboardDispatcher.Subscriber = null;
        GameMenu.forcePreventClose = false;
        Game1.closeTextEntry();
    }
}
