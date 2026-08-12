using FishingAssistant.Configuration;
using FishingAssistant.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.UI;

internal sealed class ConfigurationMenu : IClickableMenu
{
    private const int FirstOptionId = 100;
    private const int ApplyButtonId = 200;
    private const int ResetButtonId = 201;
    private const int CancelButtonId = 202;

    private readonly Func<ConfigEditSession, ConfigValidationReport> apply;
    private readonly Func<ModConfig> createDefaults;
    private readonly Func<string, string> translate;
    private readonly List<ConfigCheckbox> options = [];
    private readonly List<ClickableComponent> footerButtons = [];
    private readonly ConfigEditSession session;
    private MenuLayout layout = null!;
    private string hoverText = "";
    private string statusText = "";

    public ConfigurationMenu(
        ConfigEditSession session,
        Func<ConfigEditSession, ConfigValidationReport> apply,
        Func<ModConfig> createDefaults,
        ITranslationHelper translations)
    {
        this.session = session;
        this.apply = apply;
        this.createDefaults = createDefaults;
        this.translate = key => translations.Get(key);

        this.RebuildComponents();
        Game1.playSound("bigSelect");
    }

    public override bool areGamePadControlsImplemented()
    {
        return true;
    }

    public override bool overrideSnappyMenuCursorMovementBan()
    {
        return false;
    }

    public override bool showWithoutTransparencyIfOptionIsSet()
    {
        return true;
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        this.RebuildComponents();
    }

    public override void setUpForGamePadMode()
    {
        this.snapToDefaultClickableComponent();
        this.snapCursorToCurrentSnappedComponent();
    }

    public override void snapToDefaultClickableComponent()
    {
        this.currentlySnappedComponent = this.options[0].Component;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu != this)
            return;

        ConfigCheckbox? option = this.options.FirstOrDefault(item => item.Component.containsPoint(x, y));
        if (option is not null)
        {
            option.Toggle();
            return;
        }

        ClickableComponent? button = this.footerButtons.FirstOrDefault(item => item.containsPoint(x, y));
        if (button is null)
            return;

        switch (button.myID)
        {
            case ApplyButtonId:
                this.ApplyAndClose();
                break;
            case ResetButtonId:
                this.ResetDraft();
                break;
            case CancelButtonId:
                this.exitThisMenu();
                break;
        }
    }

    public override void receiveGamePadButton(Buttons button)
    {
        switch (button)
        {
            case Buttons.A when this.currentlySnappedComponent is not null:
                Rectangle bounds = this.currentlySnappedComponent.bounds;
                this.receiveLeftClick(bounds.Center.X, bounds.Center.Y);
                break;
            case Buttons.B:
                this.exitThisMenu();
                break;
            case Buttons.DPadUp:
            case Buttons.LeftThumbstickUp:
                this.applyMovementKey(0);
                break;
            case Buttons.DPadRight:
            case Buttons.LeftThumbstickRight:
                this.applyMovementKey(1);
                break;
            case Buttons.DPadDown:
            case Buttons.LeftThumbstickDown:
                this.applyMovementKey(2);
                break;
            case Buttons.DPadLeft:
            case Buttons.LeftThumbstickLeft:
                this.applyMovementKey(3);
                break;
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        this.hoverText = this.options
            .FirstOrDefault(item => item.Component.containsPoint(x, y))?
            .Description ?? "";
    }

    public override void draw(SpriteBatch batch)
    {
        if (!Game1.options.showMenuBackground && !Game1.options.showClearBackgrounds)
            batch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);

        Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height,
            speaker: false, drawOnlyBox: true);

        SpriteFont titleFont = this.layout.Width >= 560 ? Game1.dialogueFont : Game1.smallFont;
        string title = MenuText.Fit(this.translate("config.title"), titleFont,
            this.layout.Width - this.layout.Padding * 2);
        Vector2 titlePosition = new(
            this.layout.X + this.layout.Padding,
            this.layout.Y + (this.layout.HeaderHeight - titleFont.LineSpacing) / 2f
        );
        Utility.drawTextWithShadow(batch, title, titleFont, titlePosition, Game1.textColor);

        string section = this.translate("config.section.automation");
        Vector2 sectionSize = Game1.smallFont.MeasureString(section);
        float sectionX = this.layout.X + this.layout.Width - this.layout.Padding - sectionSize.X;
        if (titlePosition.X + titleFont.MeasureString(title).X + 16 < sectionX)
        {
            Utility.drawTextWithShadow(batch, section, Game1.smallFont,
                new Vector2(sectionX, this.layout.Y + (this.layout.HeaderHeight - Game1.smallFont.LineSpacing) / 2f),
                Game1.textColor * 0.75f);
        }

        Point mouse = new(Game1.getMouseX(), Game1.getMouseY());
        foreach (ConfigCheckbox option in this.options)
        {
            bool highlighted = option.Component.bounds.Contains(mouse)
                || this.currentlySnappedComponent == option.Component;
            option.Draw(batch, highlighted);
        }

        this.drawHorizontalPartition(batch, this.layout.ContentBottom - 12, small: true);
        foreach (ClickableComponent button in this.footerButtons)
            this.DrawButton(batch, button, button.bounds.Contains(mouse) || this.currentlySnappedComponent == button);

        base.draw(batch);
        string tooltip = string.IsNullOrEmpty(this.hoverText) ? this.statusText : this.hoverText;
        if (!string.IsNullOrEmpty(tooltip))
            drawHoverText(batch, tooltip, Game1.smallFont);

        this.drawMouse(batch);
    }

    private void ApplyAndClose()
    {
        try
        {
            this.apply(this.session);
            Game1.playSound("coin");
            this.exitThisMenu(playSound: false);
        }
        catch (InvalidOperationException exception)
        {
            this.statusText = exception.Message;
            Game1.playSound("cancel");
        }
        catch (Exception)
        {
            this.statusText = this.translate("config.status.save_failed");
            Game1.playSound("cancel");
        }
    }

    private void ResetDraft()
    {
        this.session.Draft = this.createDefaults();
        this.statusText = this.translate("config.status.defaults_loaded");
        this.RebuildComponents();
        Game1.playSound("shwip");
    }

    private void RebuildComponents()
    {
        const int optionCount = 5;
        this.layout = MenuLayout.Calculate(Game1.uiViewport.Width, Game1.uiViewport.Height, optionCount);
        this.xPositionOnScreen = this.layout.X;
        this.yPositionOnScreen = this.layout.Y;
        this.width = this.layout.Width;
        this.height = this.layout.Height;
        this.initializeUpperRightCloseButton();

        int contentX = this.layout.X + this.layout.Padding;
        int contentWidth = this.layout.Width - this.layout.Padding * 2;
        this.options.Clear();
        this.AddCheckbox(0, "config.option.auto_cast", "config.option.auto_cast.description",
            () => this.session.Draft.AutoCastFishingRod, value => this.session.Draft.AutoCastFishingRod = value,
            contentX, contentWidth);
        this.AddCheckbox(1, "config.option.auto_hook", "config.option.auto_hook.description",
            () => this.session.Draft.AutoHookFish, value => this.session.Draft.AutoHookFish = value,
            contentX, contentWidth);
        this.AddCheckbox(2, "config.option.auto_minigame", "config.option.auto_minigame.description",
            () => this.session.Draft.AutoPlayMiniGame, value => this.session.Draft.AutoPlayMiniGame = value,
            contentX, contentWidth);
        this.AddCheckbox(3, "config.option.auto_close", "config.option.auto_close.description",
            () => this.session.Draft.AutoClosePopup, value => this.session.Draft.AutoClosePopup = value,
            contentX, contentWidth);
        this.AddCheckbox(4, "config.option.auto_treasure", "config.option.auto_treasure.description",
            () => this.session.Draft.AutoLootTreasure, value => this.session.Draft.AutoLootTreasure = value,
            contentX, contentWidth);

        this.BuildFooter(contentX, contentWidth);
        this.BuildNavigation();
    }

    private void AddCheckbox(
        int index,
        string labelKey,
        string descriptionKey,
        Func<bool> getValue,
        Action<bool> setValue,
        int x,
        int width)
    {
        Rectangle bounds = new(
            x,
            this.layout.ContentTop + index * this.layout.OptionHeight,
            width,
            this.layout.OptionHeight
        );
        this.options.Add(new ConfigCheckbox(
            FirstOptionId + index,
            bounds,
            this.translate(labelKey),
            this.translate(descriptionKey),
            getValue,
            setValue
        ));
    }

    private void BuildFooter(int x, int availableWidth)
    {
        const int gap = 12;
        int buttonWidth = Math.Max(1, (availableWidth - gap * 2) / 3);
        int buttonHeight = Math.Min(52, Math.Max(1, this.layout.FooterHeight - 20));
        int y = this.layout.Y + this.layout.Height - buttonHeight - 10;

        this.footerButtons.Clear();
        this.footerButtons.Add(this.CreateButton(ApplyButtonId, x, y, buttonWidth, buttonHeight,
            this.translate("config.action.apply")));
        this.footerButtons.Add(this.CreateButton(ResetButtonId, x + buttonWidth + gap, y, buttonWidth, buttonHeight,
            this.translate("config.action.reset")));
        this.footerButtons.Add(this.CreateButton(CancelButtonId, x + (buttonWidth + gap) * 2, y, buttonWidth,
            buttonHeight, this.translate("config.action.cancel")));
    }

    private ClickableComponent CreateButton(int id, int x, int y, int width, int height, string label)
    {
        return new ClickableComponent(new Rectangle(x, y, width, height), label)
        {
            myID = id
        };
    }

    private void BuildNavigation()
    {
        for (int index = 0; index < this.options.Count; index++)
        {
            ClickableComponent component = this.options[index].Component;
            component.upNeighborID = index == 0 ? -1 : this.options[index - 1].Component.myID;
            component.downNeighborID = index == this.options.Count - 1
                ? ApplyButtonId
                : this.options[index + 1].Component.myID;
        }

        for (int index = 0; index < this.footerButtons.Count; index++)
        {
            ClickableComponent component = this.footerButtons[index];
            component.upNeighborID = this.options[^1].Component.myID;
            component.leftNeighborID = index == 0 ? -1 : this.footerButtons[index - 1].myID;
            component.rightNeighborID = index == this.footerButtons.Count - 1 ? -1 : this.footerButtons[index + 1].myID;
        }

        this.allClickableComponents = this.options.Select(option => option.Component)
            .Concat(this.footerButtons)
            .Append(this.upperRightCloseButton)
            .ToList();
    }

    private void DrawButton(SpriteBatch batch, ClickableComponent button, bool highlighted)
    {
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            button.bounds.X, button.bounds.Y, button.bounds.Width, button.bounds.Height,
            highlighted ? Color.Wheat : Color.White);

        string label = MenuText.Fit(button.name, Game1.smallFont, button.bounds.Width - 16);
        Vector2 size = Game1.smallFont.MeasureString(label);
        Vector2 position = new(
            button.bounds.Center.X - size.X / 2,
            button.bounds.Center.Y - size.Y / 2
        );
        Utility.drawTextWithShadow(batch, label, Game1.smallFont, position, Game1.textColor);
    }
}
