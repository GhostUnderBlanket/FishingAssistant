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
    private const int PreviousCategoryButtonId = 10;
    private const int NextCategoryButtonId = 11;
    private const int ScrollUpButtonId = 20;
    private const int ScrollDownButtonId = 21;
    private const int FirstOptionId = 100;
    private const int ApplyButtonId = 200;
    private const int ResetButtonId = 201;
    private const int CancelButtonId = 202;

    private readonly Func<ConfigEditSession, ConfigValidationReport> apply;
    private readonly Func<ModConfig> createDefaults;
    private readonly Func<string, string> translate;
    private readonly List<CheckboxDefinition> definitions = [];
    private readonly List<ConfigCheckbox> options = [];
    private readonly List<ClickableComponent> categoryButtons = [];
    private readonly List<ClickableComponent> scrollButtons = [];
    private readonly List<ClickableComponent> footerButtons = [];
    private readonly ConfigEditSession session;
    private ConfigCategory category;
    private MenuLayout layout = null!;
    private int scrollOffset;
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

    private int MaximumScrollOffset => Math.Max(0, this.definitions.Count - this.layout.VisibleOptionCount);

    public override bool areGamePadControlsImplemented() => true;

    public override bool showWithoutTransparencyIfOptionIsSet() => true;

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
        this.currentlySnappedComponent = this.options.Count > 0
            ? this.options[0].Component
            : this.categoryButtons[0];
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu != this)
            return;

        ClickableComponent? categoryButton = this.categoryButtons.FirstOrDefault(item => item.containsPoint(x, y));
        if (categoryButton is not null)
        {
            this.ChangeCategory(categoryButton.myID == PreviousCategoryButtonId ? -1 : 1);
            return;
        }

        ClickableComponent? scrollButton = this.scrollButtons.FirstOrDefault(item => item.containsPoint(x, y));
        if (scrollButton is not null)
        {
            this.Scroll(scrollButton.myID == ScrollUpButtonId ? -1 : 1);
            return;
        }

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

    public override void receiveScrollWheelAction(int direction)
    {
        if (direction > 0)
            this.Scroll(-1);
        else if (direction < 0)
            this.Scroll(1);
    }

    public override void receiveKeyPress(Keys key)
    {
        if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
        {
            base.receiveKeyPress(key);
            return;
        }

        if (key is Keys.Enter or Keys.Space)
        {
            this.ActivateSnappedComponent();
            return;
        }

        if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
            this.MoveUp();
        else if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
            this.applyMovementKey(1);
        else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
            this.MoveDown();
        else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
            this.applyMovementKey(3);
    }

    public override void receiveGamePadButton(Buttons button)
    {
        switch (button)
        {
            case Buttons.A:
                this.ActivateSnappedComponent();
                break;
            case Buttons.B:
                this.exitThisMenu();
                break;
            case Buttons.LeftShoulder:
                this.ChangeCategory(-1);
                break;
            case Buttons.RightShoulder:
                this.ChangeCategory(1);
                break;
            case Buttons.DPadUp:
            case Buttons.LeftThumbstickUp:
                this.MoveUp();
                break;
            case Buttons.DPadRight:
            case Buttons.LeftThumbstickRight:
                this.applyMovementKey(1);
                break;
            case Buttons.DPadDown:
            case Buttons.LeftThumbstickDown:
                this.MoveDown();
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

        this.DrawHeader(batch);
        this.DrawCategorySelector(batch);

        Point mouse = new(Game1.getMouseX(), Game1.getMouseY());
        foreach (ConfigCheckbox option in this.options)
        {
            bool highlighted = option.Component.bounds.Contains(mouse)
                || this.currentlySnappedComponent == option.Component;
            option.Draw(batch, highlighted);
        }

        foreach (ClickableComponent button in this.scrollButtons)
            this.DrawButton(batch, button, button.bounds.Contains(mouse) || this.currentlySnappedComponent == button);

        this.drawHorizontalPartition(batch, this.layout.ContentBottom - 12, small: true);
        foreach (ClickableComponent button in this.footerButtons)
            this.DrawButton(batch, button, button.bounds.Contains(mouse) || this.currentlySnappedComponent == button);

        base.draw(batch);
        string tooltip = string.IsNullOrEmpty(this.hoverText) ? this.statusText : this.hoverText;
        if (!string.IsNullOrEmpty(tooltip))
            drawHoverText(batch, tooltip, Game1.smallFont);

        this.drawMouse(batch);
    }

    private void DrawHeader(SpriteBatch batch)
    {
        SpriteFont titleFont = this.layout.Width >= 560 ? Game1.dialogueFont : Game1.smallFont;
        string title = MenuText.Fit(this.translate("config.title"), titleFont,
            this.layout.Width - this.layout.Padding * 2);
        Vector2 titlePosition = new(
            this.layout.X + this.layout.Padding,
            this.layout.Y + (this.layout.HeaderHeight - titleFont.LineSpacing) / 2f
        );
        Utility.drawTextWithShadow(batch, title, titleFont, titlePosition, Game1.textColor);
    }

    private void DrawCategorySelector(SpriteBatch batch)
    {
        Point mouse = new(Game1.getMouseX(), Game1.getMouseY());
        foreach (ClickableComponent button in this.categoryButtons)
            this.DrawButton(batch, button, button.bounds.Contains(mouse) || this.currentlySnappedComponent == button);

        string categoryName = this.translate($"config.category.{this.category.ToString().ToLowerInvariant()}");
        int left = this.categoryButtons[0].bounds.Right + 8;
        int right = this.categoryButtons[1].bounds.Left - 8;
        string label = MenuText.Fit(categoryName, Game1.smallFont, right - left);
        Vector2 size = Game1.smallFont.MeasureString(label);
        Utility.drawTextWithShadow(batch, label, Game1.smallFont,
            new Vector2((left + right) / 2f - size.X / 2f,
                this.layout.CategoryTop + (this.layout.CategoryHeight - size.Y) / 2f),
            Game1.textColor);
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

    private void ActivateSnappedComponent()
    {
        if (this.currentlySnappedComponent is null)
            this.snapToDefaultClickableComponent();
        if (this.currentlySnappedComponent is null)
            return;

        Rectangle bounds = this.currentlySnappedComponent.bounds;
        this.receiveLeftClick(bounds.Center.X, bounds.Center.Y);
    }

    private void MoveUp()
    {
        if (this.currentlySnappedComponent == this.options.FirstOrDefault()?.Component
            && this.scrollOffset > 0)
        {
            this.Scroll(-1);
            this.currentlySnappedComponent = this.options[0].Component;
            this.snapCursorToCurrentSnappedComponent();
            return;
        }

        this.applyMovementKey(0);
    }

    private void MoveDown()
    {
        if (this.currentlySnappedComponent == this.options.LastOrDefault()?.Component
            && this.scrollOffset < this.MaximumScrollOffset)
        {
            this.Scroll(1);
            this.currentlySnappedComponent = this.options[^1].Component;
            this.snapCursorToCurrentSnappedComponent();
            return;
        }

        this.applyMovementKey(2);
    }

    private void ResetDraft()
    {
        this.session.Draft = this.createDefaults();
        this.statusText = this.translate("config.status.defaults_loaded");
        this.RebuildOptions();
        Game1.playSound("shwip");
    }

    private void ChangeCategory(int direction)
    {
        ConfigCategory[] categories = Enum.GetValues<ConfigCategory>();
        int index = Array.IndexOf(categories, this.category);
        index = (index + direction + categories.Length) % categories.Length;
        this.category = categories[index];
        this.scrollOffset = 0;
        this.statusText = "";
        this.RebuildOptions();
        Game1.playSound("smallSelect");
    }

    private void Scroll(int direction)
    {
        int offset = Math.Clamp(this.scrollOffset + direction, 0, this.MaximumScrollOffset);
        if (offset == this.scrollOffset)
            return;

        int? selectedId = this.currentlySnappedComponent?.myID;
        this.scrollOffset = offset;
        this.RebuildVisibleOptions();
        this.BuildNavigation();
        this.RestoreSelection(selectedId);
        Game1.playSound("shwip");
    }

    private void RebuildComponents()
    {
        this.layout = MenuLayout.Calculate(Game1.uiViewport.Width, Game1.uiViewport.Height);
        this.xPositionOnScreen = this.layout.X;
        this.yPositionOnScreen = this.layout.Y;
        this.width = this.layout.Width;
        this.height = this.layout.Height;
        this.initializeUpperRightCloseButton();

        this.BuildCategoryButtons();
        this.BuildFooter();
        this.RebuildOptions();
    }

    private void RebuildOptions()
    {
        int? selectedId = this.currentlySnappedComponent?.myID;
        this.BuildDefinitions();
        this.scrollOffset = Math.Clamp(this.scrollOffset, 0, this.MaximumScrollOffset);
        this.RebuildVisibleOptions();
        this.BuildNavigation();
        this.RestoreSelection(selectedId);
    }

    private void RebuildVisibleOptions()
    {
        int contentX = this.layout.X + this.layout.Padding;
        int contentWidth = this.layout.Width - this.layout.Padding * 2;
        bool showScroll = this.MaximumScrollOffset > 0;
        int scrollWidth = showScroll ? Math.Min(44, contentWidth / 5) : 0;
        int optionWidth = contentWidth - scrollWidth;

        this.options.Clear();
        IEnumerable<(CheckboxDefinition Definition, int Index)> visible = this.definitions
            .Select((definition, index) => (definition, index))
            .Skip(this.scrollOffset)
            .Take(this.layout.VisibleOptionCount);
        int row = 0;
        foreach ((CheckboxDefinition definition, int index) in visible)
        {
            Rectangle bounds = new(
                contentX,
                this.layout.ContentTop + row * this.layout.OptionHeight,
                optionWidth,
                this.layout.OptionHeight
            );
            this.options.Add(new ConfigCheckbox(
                FirstOptionId + index,
                bounds,
                this.translate(definition.LabelKey),
                this.translate(definition.DescriptionKey),
                definition.GetValue,
                definition.SetValue
            ));
            row++;
        }

        this.scrollButtons.Clear();
        if (showScroll)
        {
            int buttonHeight = Math.Min(44, Math.Max(1, this.layout.OptionHeight));
            int x = contentX + optionWidth;
            this.scrollButtons.Add(this.CreateButton(ScrollUpButtonId, x, this.layout.ContentTop,
                scrollWidth, buttonHeight, "^"));
            this.scrollButtons.Add(this.CreateButton(ScrollDownButtonId, x,
                this.layout.ContentBottom - buttonHeight, scrollWidth, buttonHeight, "v"));
        }
    }

    private void BuildDefinitions()
    {
        this.definitions.Clear();
        switch (this.category)
        {
            case ConfigCategory.Automation:
                this.AddDefinition("auto_cast", () => this.session.Draft.AutoCastFishingRod,
                    value => this.session.Draft.AutoCastFishingRod = value);
                this.AddDefinition("auto_hook", () => this.session.Draft.AutoHookFish,
                    value => this.session.Draft.AutoHookFish = value);
                this.AddDefinition("auto_minigame", () => this.session.Draft.AutoPlayMiniGame,
                    value => this.session.Draft.AutoPlayMiniGame = value);
                this.AddDefinition("auto_close", () => this.session.Draft.AutoClosePopup,
                    value => this.session.Draft.AutoClosePopup = value);
                this.AddDefinition("auto_treasure", () => this.session.Draft.AutoLootTreasure,
                    value => this.session.Draft.AutoLootTreasure = value);
                break;
            case ConfigCategory.Inventory:
                this.AddDefinition("auto_trash", () => this.session.Draft.AutoTrashJunk,
                    value => this.session.Draft.AutoTrashJunk = value);
                this.AddDefinition("trash_fish", () => this.session.Draft.AllowTrashFish,
                    value => this.session.Draft.AllowTrashFish = value);
                this.AddDefinition("auto_eat", () => this.session.Draft.AutoEatFood,
                    value => this.session.Draft.AutoEatFood = value);
                this.AddDefinition("eat_fish", () => this.session.Draft.AllowEatingFish,
                    value => this.session.Draft.AllowEatingFish = value);
                break;
            case ConfigCategory.Equipment:
                this.AddDefinition("attach_bait", () => this.session.Draft.AutoAttachBait,
                    value => this.session.Draft.AutoAttachBait = value);
                this.AddDefinition("spawn_bait", () => this.session.Draft.SpawnBaitIfDontHave,
                    value => this.session.Draft.SpawnBaitIfDontHave = value);
                this.AddDefinition("attach_tackle", () => this.session.Draft.AutoAttachTackles,
                    value => this.session.Draft.AutoAttachTackles = value);
                this.AddDefinition("spawn_tackle", () => this.session.Draft.SpawnTackleIfDontHave,
                    value => this.session.Draft.SpawnTackleIfDontHave = value);
                this.AddDefinition("infinite_bait", () => this.session.Draft.InfiniteBait,
                    value => this.session.Draft.InfiniteBait = value);
                this.AddDefinition("infinite_tackle", () => this.session.Draft.InfiniteTackle,
                    value => this.session.Draft.InfiniteTackle = value);
                break;
            case ConfigCategory.Fishing:
                this.AddDefinition("instant_bite", () => this.session.Draft.InstantFishBite,
                    value => this.session.Draft.InstantFishBite = value);
                this.AddDefinition("always_perfect", () => this.session.Draft.AlwaysPerfect,
                    value => this.session.Draft.AlwaysPerfect = value);
                this.AddDefinition("max_fish_size", () => this.session.Draft.AlwaysMaxFishSize,
                    value => this.session.Draft.AlwaysMaxFishSize = value);
                this.AddDefinition("instant_treasure", () => this.session.Draft.InstantCatchTreasure,
                    value => this.session.Draft.InstantCatchTreasure = value);
                break;
            case ConfigCategory.Display:
                this.AddDefinition("fish_preview", () => this.session.Draft.DisplayFishPreview,
                    value => this.session.Draft.DisplayFishPreview = value);
                this.AddDefinition("fish_name", () => this.session.Draft.ShowFishName,
                    value => this.session.Draft.ShowFishName = value);
                this.AddDefinition("show_treasure", () => this.session.Draft.ShowTreasure,
                    value => this.session.Draft.ShowTreasure = value);
                this.AddDefinition("uncaught_fish", () => this.session.Draft.ShowUncaughtFish,
                    value => this.session.Draft.ShowUncaughtFish = value);
                this.AddDefinition("legendary_fish", () => this.session.Draft.ShowLegendaryFish,
                    value => this.session.Draft.ShowLegendaryFish = value);
                break;
            case ConfigCategory.Enchantments:
                this.AddDefinition("enchant_auto_hook", () => this.session.Draft.AddAutoHookEnchantment,
                    value => this.session.Draft.AddAutoHookEnchantment = value);
                this.AddDefinition("enchant_efficient", () => this.session.Draft.AddEfficientEnchantment,
                    value => this.session.Draft.AddEfficientEnchantment = value);
                this.AddDefinition("enchant_master", () => this.session.Draft.AddMasterEnchantment,
                    value => this.session.Draft.AddMasterEnchantment = value);
                this.AddDefinition("enchant_preserving", () => this.session.Draft.AddPreservingEnchantment,
                    value => this.session.Draft.AddPreservingEnchantment = value);
                this.AddDefinition("remove_enchantments", () => this.session.Draft.RemoveWhenUnequipped,
                    value => this.session.Draft.RemoveWhenUnequipped = value);
                break;
        }
    }

    private void AddDefinition(string key, Func<bool> getValue, Action<bool> setValue)
    {
        this.definitions.Add(new CheckboxDefinition(
            $"config.option.{key}",
            $"config.option.{key}.description",
            getValue,
            setValue
        ));
    }

    private void BuildCategoryButtons()
    {
        int x = this.layout.X + this.layout.Padding;
        int width = this.layout.Width - this.layout.Padding * 2;
        int buttonSize = Math.Min(this.layout.CategoryHeight, 44);
        int y = this.layout.CategoryTop + (this.layout.CategoryHeight - buttonSize) / 2;

        this.categoryButtons.Clear();
        this.categoryButtons.Add(this.CreateButton(PreviousCategoryButtonId, x, y, buttonSize, buttonSize, "<"));
        this.categoryButtons.Add(this.CreateButton(NextCategoryButtonId, x + width - buttonSize, y,
            buttonSize, buttonSize, ">"));
    }

    private void BuildFooter()
    {
        const int gap = 12;
        int x = this.layout.X + this.layout.Padding;
        int availableWidth = this.layout.Width - this.layout.Padding * 2;
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
        ClickableComponent previousCategory = this.categoryButtons[0];
        ClickableComponent nextCategory = this.categoryButtons[1];
        previousCategory.rightNeighborID = nextCategory.myID;
        nextCategory.leftNeighborID = previousCategory.myID;

        int firstOptionId = this.options.FirstOrDefault()?.Component.myID ?? ApplyButtonId;
        previousCategory.downNeighborID = firstOptionId;
        nextCategory.downNeighborID = firstOptionId;

        for (int index = 0; index < this.options.Count; index++)
        {
            ClickableComponent component = this.options[index].Component;
            component.upNeighborID = index == 0 ? PreviousCategoryButtonId : this.options[index - 1].Component.myID;
            component.downNeighborID = index == this.options.Count - 1
                ? ApplyButtonId
                : this.options[index + 1].Component.myID;
        }

        int lastOptionId = this.options.LastOrDefault()?.Component.myID ?? PreviousCategoryButtonId;
        for (int index = 0; index < this.footerButtons.Count; index++)
        {
            ClickableComponent component = this.footerButtons[index];
            component.upNeighborID = lastOptionId;
            component.leftNeighborID = index == 0 ? -1 : this.footerButtons[index - 1].myID;
            component.rightNeighborID = index == this.footerButtons.Count - 1 ? -1 : this.footerButtons[index + 1].myID;
        }

        this.allClickableComponents = this.categoryButtons
            .Concat(this.options.Select(option => option.Component))
            .Concat(this.scrollButtons)
            .Concat(this.footerButtons)
            .Append(this.upperRightCloseButton)
            .ToList();
    }

    private void DrawButton(SpriteBatch batch, ClickableComponent button, bool highlighted)
    {
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            button.bounds.X, button.bounds.Y, button.bounds.Width, button.bounds.Height,
            highlighted ? Color.Wheat : Color.White);

        string label = MenuText.Fit(button.name, Game1.smallFont, button.bounds.Width - 12);
        Vector2 size = Game1.smallFont.MeasureString(label);
        Vector2 position = new(
            button.bounds.Center.X - size.X / 2,
            button.bounds.Center.Y - size.Y / 2
        );
        Utility.drawTextWithShadow(batch, label, Game1.smallFont, position, Game1.textColor);
    }

    private void RestoreSelection(int? selectedId)
    {
        if (selectedId is null)
            return;

        this.currentlySnappedComponent = this.allClickableComponents
            .FirstOrDefault(component => component.myID == selectedId)
            ?? this.options.FirstOrDefault()?.Component
            ?? this.categoryButtons[0];
    }

    private sealed record CheckboxDefinition(
        string LabelKey,
        string DescriptionKey,
        Func<bool> GetValue,
        Action<bool> SetValue
    );
}
