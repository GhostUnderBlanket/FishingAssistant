using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.UI;

internal sealed class ConfigApplyFeedbackDialog : IClickableMenu
{
    private const int DoneButtonId = 1;
    private const int HorizontalMargin = 32;
    private const int VerticalMargin = 32;
    private const int DialogPadding = 48;
    private const int FooterHeight = 88;
    private const int MinimumWidth = 280;
    private const int MaximumWidth = 720;

    private readonly string unwrappedMessage;
    private readonly string doneLabel;
    private string message = "";
    private ClickableComponent doneButton = null!;
    private bool closing;

    public ConfigApplyFeedbackDialog(string message, string doneLabel, Action afterClose)
    {
        this.unwrappedMessage = message;
        this.doneLabel = doneLabel;
        this.exitFunction = afterClose.Invoke;
        this.RebuildLayout();
    }

    public override bool areGamePadControlsImplemented() => true;

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        this.RebuildLayout();
    }

    public override void snapToDefaultClickableComponent()
    {
        this.currentlySnappedComponent = this.doneButton;
        this.snapCursorToCurrentSnappedComponent();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.doneButton.containsPoint(x, y))
            this.Close();
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key is Keys.Enter or Keys.Space or Keys.Escape or Keys.Y or Keys.N)
            this.Close();
    }

    public override void receiveGamePadButton(Buttons button)
    {
        if (button is Buttons.A or Buttons.B or Buttons.Start)
            this.Close();
    }

    public override void performHoverAction(int x, int y)
    {
        this.doneButton.scale = this.doneButton.containsPoint(x, y) ? 1.05f : 1f;
    }

    public override void draw(SpriteBatch batch)
    {
        batch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
        Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height,
            speaker: false, drawOnlyBox: true);

        batch.DrawString(Game1.dialogueFont, this.message,
            new Vector2(this.xPositionOnScreen + DialogPadding, this.yPositionOnScreen + DialogPadding),
            Game1.textColor);

        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            this.doneButton.bounds.X, this.doneButton.bounds.Y,
            this.doneButton.bounds.Width, this.doneButton.bounds.Height,
            this.doneButton.scale > 1f || this.currentlySnappedComponent == this.doneButton
                ? Color.Wheat
                : Color.White);

        string label = MenuText.Fit(this.doneLabel, Game1.smallFont, this.doneButton.bounds.Width - 16);
        Vector2 labelSize = Game1.smallFont.MeasureString(label);
        Utility.drawTextWithShadow(batch, label, Game1.smallFont,
            new Vector2(this.doneButton.bounds.Center.X - labelSize.X / 2f,
                this.doneButton.bounds.Center.Y - labelSize.Y / 2f),
            Game1.textColor);
        this.drawMouse(batch);
    }

    private void RebuildLayout()
    {
        int availableWidth = Math.Max(1, Game1.uiViewport.Width - HorizontalMargin * 2);
        int textWidth = Math.Max(1, Math.Min(MaximumWidth - DialogPadding * 2,
            availableWidth - DialogPadding * 2));
        this.message = Game1.parseText(this.unwrappedMessage, Game1.dialogueFont, textWidth);
        Vector2 messageSize = Game1.dialogueFont.MeasureString(this.message);

        this.width = Math.Min(availableWidth,
            Math.Max(Math.Min(MinimumWidth, availableWidth), (int)Math.Ceiling(messageSize.X) + DialogPadding * 2));
        int desiredHeight = (int)Math.Ceiling(messageSize.Y) + DialogPadding * 2 + FooterHeight;
        this.height = Math.Min(Math.Max(1, Game1.uiViewport.Height - VerticalMargin * 2), desiredHeight);
        this.xPositionOnScreen = (Game1.uiViewport.Width - this.width) / 2;
        this.yPositionOnScreen = (Game1.uiViewport.Height - this.height) / 2;

        int buttonWidth = Math.Min(180, Math.Max(1, this.width - DialogPadding * 2));
        int buttonHeight = Math.Min(56, Math.Max(1, this.height - DialogPadding * 2));
        this.doneButton = new ClickableComponent(
            new Rectangle(this.xPositionOnScreen + (this.width - buttonWidth) / 2,
                this.yPositionOnScreen + this.height - DialogPadding - buttonHeight,
                buttonWidth,
                buttonHeight),
            this.doneLabel)
        {
            myID = DoneButtonId
        };
        this.allClickableComponents = [this.doneButton];

        if (Game1.options.SnappyMenus)
            this.snapToDefaultClickableComponent();
    }

    private void Close()
    {
        if (this.closing)
            return;

        this.closing = true;
        Game1.playSound("smallSelect");
        this.exitThisMenu(playSound: false);
    }
}
