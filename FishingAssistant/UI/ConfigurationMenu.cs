using FishingAssistant.Configuration;
using FishingAssistant.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
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
    private const float InlineMessageScale = 0.72f;

    private readonly Func<ConfigEditSession, ConfigValidationReport> apply;
    private readonly Func<string, string> translate;
    private readonly IConfigItemSource itemSource;
    private readonly Action addTestFishingRod;
    private readonly Action warpToBeachFishingSpot;
    private readonly ConfigResetWorkflow resetWorkflow;
    private readonly List<ControlDefinition> definitions = [];
    private readonly List<IConfigControl> options = [];
    private readonly Dictionary<int, string> optionKeys = [];
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
        IConfigItemSource itemSource,
        ITranslationHelper translations,
        Action addTestFishingRod,
        Action warpToBeachFishingSpot)
    {
        this.session = session;
        this.apply = apply;
        this.itemSource = itemSource;
        this.translate = key => translations.Get(key);
        this.addTestFishingRod = addTestFishingRod;
        this.warpToBeachFishingSpot = warpToBeachFishingSpot;
        this.resetWorkflow = new ConfigResetWorkflow(createDefaults);

        this.RebuildComponents();
        Game1.playSound("bigSelect");
    }

    private int MaximumScrollOffset => Math.Max(0, this.definitions.Count - this.layout.VisibleOptionCount);

    public bool IsListeningForKeybind => this.options.OfType<ConfigKeybind>().Any(control => control.IsListening);

    public void ReceiveKeybindInput(IReadOnlyList<SButton> buttons)
    {
        this.options.OfType<ConfigKeybind>().FirstOrDefault(control => control.IsListening)?.Capture(buttons);
    }

    public override bool areGamePadControlsImplemented() => true;

    public override bool showWithoutTransparencyIfOptionIsSet() => true;

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        this.CancelKeybindListening();
        this.RebuildComponents();
        this.GetChildMenu()?.gameWindowSizeChanged(oldBounds, newBounds);
    }

    public override bool readyToClose() => !this.IsListeningForKeybind;

    public override void emergencyShutDown()
    {
        this.resetWorkflow.Cancel();
        this.CancelKeybindListening();
        base.emergencyShutDown();
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

        IConfigControl? option = this.options.FirstOrDefault(item => item.Component.containsPoint(x, y));
        if (option is not null)
        {
            option.ReceiveLeftClick(x, y);
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
                this.RequestResetDraft();
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
        if (this.IsListeningForKeybind)
            return;

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
            this.MoveHorizontal(1);
        else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
            this.MoveDown();
        else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
            this.MoveHorizontal(-1);
    }

    public override void receiveGamePadButton(Buttons button)
    {
        if (this.IsListeningForKeybind)
            return;

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
                this.MoveHorizontal(1);
                break;
            case Buttons.DPadDown:
            case Buttons.LeftThumbstickDown:
                this.MoveDown();
                break;
            case Buttons.DPadLeft:
            case Buttons.LeftThumbstickLeft:
                this.MoveHorizontal(-1);
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
        IReadOnlyDictionary<string, InlineConfigMessage> inlineMessages = InlineConfigValidation
            .Evaluate(this.session.Draft)
            .ToDictionary(message => message.OptionKey, StringComparer.Ordinal);
        foreach (IConfigControl option in this.options)
        {
            bool highlighted = option.Component.bounds.Contains(mouse)
                || this.currentlySnappedComponent == option.Component;
            string? optionKey = this.optionKeys.GetValueOrDefault(option.Component.myID);
            InlineConfigMessage? inlineMessage = optionKey is null
                ? null
                : inlineMessages.GetValueOrDefault(optionKey);
            this.DrawOption(batch, option, highlighted, inlineMessage);
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

    protected override void cleanupBeforeExit()
    {
        this.CancelKeybindListening();
        base.cleanupBeforeExit();
    }

    private void CancelKeybindListening()
    {
        foreach (ConfigKeybind control in this.options.OfType<ConfigKeybind>())
            control.CancelListening();

        GameMenu.forcePreventClose = false;
    }

    private void DrawHeader(SpriteBatch batch)
    {
        SpriteFont titleFont = this.layout.Width >= 560 ? Game1.dialogueFont : Game1.smallFont;
        string title = MenuText.Fit(this.translate("config.title"), titleFont,
            this.layout.Width - this.layout.Padding * 2);
        Vector2 titleSize = titleFont.MeasureString(title);
        Vector2 titlePosition = new(
            this.layout.X + this.layout.Padding,
            this.layout.Y + (this.layout.HeaderHeight - titleFont.LineSpacing) / 2f
        );
        int backgroundPadding = 14;
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            (int)titlePosition.X - backgroundPadding,
            (int)titlePosition.Y - 6,
            (int)titleSize.X + backgroundPadding * 2,
            titleFont.LineSpacing + 12,
            Color.White);
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

    private void MoveHorizontal(int direction)
    {
        IConfigControl? selected = this.options
            .FirstOrDefault(option => option.Component == this.currentlySnappedComponent);
        if (selected?.Adjust(direction) == true)
            return;

        this.applyMovementKey(direction > 0 ? 1 : 3);
    }

    private void RequestResetDraft()
    {
        this.resetWorkflow.Request();
        int messageWidth = ResetConfirmationLayout.GetTextWidth(Game1.uiViewport.Width);
        string message = Game1.parseText(
            this.translate("config.confirm.reset"),
            Game1.dialogueFont,
            messageWidth);
        ConfirmationDialog? dialog = null;
        dialog = new ConfirmationDialog(
            message,
            _ =>
            {
                ModConfig? defaults = this.resetWorkflow.Confirm();
                dialog!.exitThisMenu(playSound: false);
                if (defaults is not null)
                    this.ResetDraft(defaults);
            },
            _ => dialog!.exitThisMenu(playSound: false));
        dialog.behaviorBeforeCleanup = _ => this.resetWorkflow.Cancel();
        this.SetChildMenu(dialog);
    }

    private void ResetDraft(ModConfig defaults)
    {
        this.session.Draft = defaults;
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
        this.optionKeys.Clear();
        IEnumerable<(ControlDefinition Definition, int Index)> visible = this.definitions
            .Select((definition, index) => (definition, index))
            .Skip(this.scrollOffset)
            .Take(this.layout.VisibleOptionCount);
        int row = 0;
        foreach ((ControlDefinition definition, int index) in visible)
        {
            Rectangle bounds = new(
                contentX,
                this.layout.ContentTop + row * this.layout.OptionHeight,
                optionWidth,
                this.layout.OptionHeight
            );
            IConfigControl option = definition.Create(FirstOptionId + index, bounds);
            this.options.Add(option);
            this.optionKeys[option.Component.myID] = definition.Key;
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
                this.AddEnumDefinition("auto_pause", () => this.session.Draft.AutoPauseFishing,
                    value => this.session.Draft.AutoPauseFishing = value);
                this.AddNumberDefinition("pause_time", () => this.session.Draft.TimeToPause,
                    value => this.session.Draft.TimeToPause = Convert.ToInt32(value), 6, 25, 1);
                this.AddNumberDefinition("warning_count", () => this.session.Draft.WarnCount,
                    value => this.session.Draft.WarnCount = Convert.ToInt32(value), 1, 5, 1);
                break;
            case ConfigCategory.Inventory:
                this.AddEnumDefinition("inventory_full_action", () => this.session.Draft.ActionIfInventoryFull,
                    value => this.session.Draft.ActionIfInventoryFull = value);
                this.AddDefinition("auto_trash", () => this.session.Draft.AutoTrashJunk,
                    value => this.session.Draft.AutoTrashJunk = value);
                this.AddActionDefinition("junk_lists",
                    () => string.Format(this.translate("config.junk_picker.selected"),
                        this.session.Draft.JunkList.Count,
                        this.session.Draft.JunkIgnoreList.Count),
                    () => this.SetChildMenu(new JunkListMenu(
                        this.session.Draft.JunkList,
                        this.session.Draft.JunkIgnoreList,
                        this.itemSource,
                        this.translate)));
                this.AddDefinition("trash_fish", () => this.session.Draft.AllowTrashFish,
                    value => this.session.Draft.AllowTrashFish = value);
                this.AddDefinition("auto_eat", () => this.session.Draft.AutoEatFood,
                    value => this.session.Draft.AutoEatFood = value);
                this.AddNumberDefinition("eat_energy", () => this.session.Draft.EnergyPercentToEat,
                    value => this.session.Draft.EnergyPercentToEat = Convert.ToInt32(value), 5, 95, 5,
                    value => $"{value:0}%");
                this.AddDefinition("eat_fish", () => this.session.Draft.AllowEatingFish,
                    value => this.session.Draft.AllowEatingFish = value);
                break;
            case ConfigCategory.Equipment:
                this.AddDefinition("attach_bait", () => this.session.Draft.AutoAttachBait,
                    value => this.session.Draft.AutoAttachBait = value);
                this.AddDefinition("spawn_bait", () => this.session.Draft.SpawnBaitIfDontHave,
                    value => this.session.Draft.SpawnBaitIfDontHave = value);
                this.AddNumberDefinition("bait_amount", () => this.session.Draft.BaitAmountToSpawn,
                    value => this.session.Draft.BaitAmountToSpawn = Convert.ToInt32(value), 1, 999, 1);
                this.AddItemDefinition("preferred_bait", ConfigItemKind.Bait, "Any",
                    () => this.session.Draft.PreferredBait, value => this.session.Draft.PreferredBait = value);
                this.AddDefinition("attach_tackle", () => this.session.Draft.AutoAttachTackles,
                    value => this.session.Draft.AutoAttachTackles = value);
                this.AddItemDefinition("preferred_tackle", ConfigItemKind.Tackle, "Any",
                    () => this.session.Draft.PreferredTackle, value => this.session.Draft.PreferredTackle = value);
                this.AddItemDefinition("second_tackle", ConfigItemKind.Tackle, "Any",
                    () => this.session.Draft.PreferredAdvIridiumTackle,
                    value => this.session.Draft.PreferredAdvIridiumTackle = value);
                this.AddDefinition("spawn_tackle", () => this.session.Draft.SpawnTackleIfDontHave,
                    value => this.session.Draft.SpawnTackleIfDontHave = value);
                this.AddDefinition("infinite_bait", () => this.session.Draft.InfiniteBait,
                    value => this.session.Draft.InfiniteBait = value);
                this.AddDefinition("infinite_tackle", () => this.session.Draft.InfiniteTackle,
                    value => this.session.Draft.InfiniteTackle = value);
                break;
            case ConfigCategory.Fishing:
                this.AddEnumDefinition("skip_minigame", () => this.session.Draft.SkipFishingMiniGame,
                    value => this.session.Draft.SkipFishingMiniGame = value);
                this.AddDefinition("instant_bite", () => this.session.Draft.InstantFishBite,
                    value => this.session.Draft.InstantFishBite = value);
                this.AddNumberDefinition("fish_amount", () => this.session.Draft.PreferFishAmount,
                    value => this.session.Draft.PreferFishAmount = Convert.ToInt32(value), 1, 3, 1);
                this.AddEnumDefinition("fish_quality", () => this.session.Draft.PreferFishQuality,
                    value => this.session.Draft.PreferFishQuality = value);
                this.AddDefinition("always_perfect", () => this.session.Draft.AlwaysPerfect,
                    value => this.session.Draft.AlwaysPerfect = value);
                this.AddDefinition("max_fish_size", () => this.session.Draft.AlwaysMaxFishSize,
                    value => this.session.Draft.AlwaysMaxFishSize = value);
                this.AddNumberDefinition("difficulty_multiplier", () => this.session.Draft.FishDifficultyMultiplier,
                    value => this.session.Draft.FishDifficultyMultiplier = (float)value, 0, 10, 0.1,
                    value => $"{value:0.0}x");
                this.AddNumberDefinition("difficulty_additive", () => this.session.Draft.FishDifficultyAdditive,
                    value => this.session.Draft.FishDifficultyAdditive = Convert.ToInt32(value), -100, 100, 5,
                    value => $"{value:+0;-0;0}");
                this.AddDefinition("instant_treasure", () => this.session.Draft.InstantCatchTreasure,
                    value => this.session.Draft.InstantCatchTreasure = value);
                this.AddDefinition("treasure_targeting", () => this.session.Draft.TreasureTargeting,
                    value => this.session.Draft.TreasureTargeting = value);
                this.AddEnumDefinition("treasure_chance", () => this.session.Draft.TreasureChance,
                    value => this.session.Draft.TreasureChance = value);
                this.AddEnumDefinition("golden_treasure_chance", () => this.session.Draft.GoldenTreasureChance,
                    value => this.session.Draft.GoldenTreasureChance = value);
                this.AddNumberDefinition("cast_power", () => this.session.Draft.DefaultCastPower,
                    value => this.session.Draft.DefaultCastPower = Convert.ToInt32(value), 0, 100, 5,
                    value => $"{value:0}%");
                this.AddNumberDefinition("cast_delay", () => this.session.Draft.AutoCastDelaySeconds,
                    value => this.session.Draft.AutoCastDelaySeconds = (float)value, 0, 10, 0.25,
                    value => $"{value:0.##}s");
                this.AddNumberDefinition("unlock_cast_time", () => this.session.Draft.UnlockCastPowerTime,
                    value => this.session.Draft.UnlockCastPowerTime = (float)value, 0, 3, 0.1,
                    value => $"{value:0.0}s");
                this.AddItemDefinition("starter_rod", ConfigItemKind.FishingRod, "None",
                    () => this.session.Draft.StartWithFishingRod,
                    value => this.session.Draft.StartWithFishingRod = value,
                    "config.value.no_starter_rod");
                break;
            case ConfigCategory.Display:
                this.AddEnumDefinition("hud_position", () => this.session.Draft.ModStatusPosition,
                    value => this.session.Draft.ModStatusPosition = value);
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
            case ConfigCategory.Controls:
                this.AddKeybindDefinition("toggle_automation", () => this.session.Draft.EnableAutomationButton,
                    value => this.session.Draft.EnableAutomationButton = value);
                this.AddKeybindDefinition("open_config", () => this.session.Draft.OpenConfigMenuButton,
                    value => this.session.Draft.OpenConfigMenuButton = value);
                break;
            case ConfigCategory.Debug:
                this.AddActionDefinition("add_test_rod",
                    () => this.translate("config.action.add"),
                    this.addTestFishingRod);
                this.AddActionDefinition("warp_beach",
                    () => this.translate("config.action.warp"),
                    this.warpToBeachFishingSpot);
                break;
        }
    }

    private void AddDefinition(string key, Func<bool> getValue, Action<bool> setValue)
    {
        this.definitions.Add(new ControlDefinition(key, (id, bounds) => new ConfigCheckbox(
            id,
            bounds,
            this.translate($"config.option.{key}"),
            this.translate($"config.option.{key}.description"),
            getValue,
            setValue
        )));
    }

    private void AddEnumDefinition<TEnum>(string key, Func<TEnum> getValue, Action<TEnum> setValue)
        where TEnum : struct, Enum
    {
        TEnum[] values = Enum.GetValues<TEnum>();
        this.definitions.Add(new ControlDefinition(key, (id, bounds) => new ConfigValueSelector<TEnum>(
            id,
            bounds,
            this.translate($"config.option.{key}"),
            this.translate($"config.option.{key}.description"),
            getValue,
            setValue,
            (current, direction) => OptionAdjustment.Cycle(values, current, direction),
            value => this.translate($"config.value.{value.ToString().ToLowerInvariant()}")
        )));
    }

    private void AddNumberDefinition(
        string key,
        Func<double> getValue,
        Action<double> setValue,
        double minimum,
        double maximum,
        double increment,
        Func<double, string>? format = null)
    {
        this.definitions.Add(new ControlDefinition(key, (id, bounds) => new ConfigValueSelector<double>(
            id,
            bounds,
            this.translate($"config.option.{key}"),
            this.translate($"config.option.{key}.description"),
            getValue,
            setValue,
            (current, direction) => OptionAdjustment.Step(current, direction, increment, minimum, maximum),
            format ?? (value => value.ToString("0.##"))
        )));
    }

    private void AddItemDefinition(
        string key,
        ConfigItemKind kind,
        string sentinel,
        Func<string> getValue,
        Action<string> setValue,
        string? sentinelLabelKey = null)
    {
        ConfigItem[] items = this.itemSource.GetAll(kind).ToArray();
        Dictionary<string, string> labels = items.ToDictionary(
            item => item.QualifiedItemId,
            item => item.DisplayName,
            StringComparer.OrdinalIgnoreCase
        );
        labels[sentinel] = this.translate(
            sentinelLabelKey ?? $"config.value.{sentinel.ToLowerInvariant()}"
        );

        this.definitions.Add(new ControlDefinition(key, (id, bounds) => new ConfigItemPicker(
            id,
            bounds,
            this.translate($"config.option.{key}"),
            this.translate($"config.option.{key}.description"),
            getValue,
            value => labels.GetValueOrDefault(value, value),
            () => this.SetChildMenu(new SingleItemPickerMenu(
                this.translate($"config.option.{key}"),
                items,
                sentinel,
                labels[sentinel],
                getValue,
                setValue,
                this.translate))
        )));
    }

    private void AddKeybindDefinition(
        string key,
        Func<KeybindList> getValue,
        Action<KeybindList> setValue)
    {
        this.definitions.Add(new ControlDefinition(key, (id, bounds) => new ConfigKeybind(
            id,
            bounds,
            this.translate($"config.option.{key}"),
            this.translate($"config.option.{key}.description"),
            this.translate("config.keybind.listening"),
            getValue,
            setValue
        )));
    }

    private void AddActionDefinition(string key, Func<string> getButtonLabel, Action activate)
    {
        this.definitions.Add(new ControlDefinition(key, (id, bounds) => new ConfigActionButton(
            id,
            bounds,
            this.translate($"config.option.{key}"),
            this.translate($"config.option.{key}.description"),
            getButtonLabel,
            activate
        )));
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

        if (this.TryDrawArrow(batch, button))
            return;

        string label = MenuText.Fit(button.name, Game1.smallFont, button.bounds.Width - 12);
        Vector2 size = Game1.smallFont.MeasureString(label);
        Vector2 position = new(
            button.bounds.Center.X - size.X / 2,
            button.bounds.Center.Y - size.Y / 2
        );
        Utility.drawTextWithShadow(batch, label, Game1.smallFont, position, Game1.textColor);
    }

    private void DrawOption(
        SpriteBatch batch,
        IConfigControl option,
        bool highlighted,
        InlineConfigMessage? inlineMessage)
    {
        Rectangle originalBounds = option.Component.bounds;
        int inlineLineHeight = (int)Math.Ceiling(Game1.smallFont.LineSpacing * InlineMessageScale);
        int messageHeight = inlineMessage is null
            ? 0
            : Math.Min(inlineLineHeight + 4, Math.Max(0, originalBounds.Height / 2));
        bool hasInlineSpace = messageHeight >= inlineLineHeight;
        option.Draw(batch, highlighted, hasInlineSpace ? messageHeight : 0);
        if (inlineMessage is null)
            return;

        float y = hasInlineSpace
            ? originalBounds.Bottom - messageHeight + 1
            : originalBounds.Bottom - inlineLineHeight;
        Rectangle messageBounds = new(
            originalBounds.X + 4,
            (int)y,
            Math.Max(1, option.InlineMessageRight - originalBounds.X - 4),
            Math.Min(inlineLineHeight + 2, Math.Max(1, originalBounds.Bottom - (int)y)));
        batch.Draw(Game1.staminaRect, messageBounds, MenuVisualMetrics.InlineMessageBackground);
        batch.Draw(Game1.staminaRect,
            new Rectangle(messageBounds.X, messageBounds.Y, Math.Min(4, messageBounds.Width), messageBounds.Height),
            MenuVisualMetrics.InlineMessageAccent);

        string warning = this.translate(inlineMessage.TranslationKey);
        string fitted = MenuText.Fit(warning, Game1.smallFont,
            Math.Max(1, messageBounds.Width - 16) / InlineMessageScale);
        batch.DrawString(Game1.smallFont, fitted,
            new Vector2(messageBounds.X + 10, messageBounds.Y),
            MenuVisualMetrics.InlineMessageText,
            0f,
            Vector2.Zero,
            InlineMessageScale,
            SpriteEffects.None,
            0.9f);
    }

    private bool TryDrawArrow(SpriteBatch batch, ClickableComponent button)
    {
        float? rotation = button.myID switch
        {
            PreviousCategoryButtonId => -MathF.PI / 2f,
            NextCategoryButtonId => MathF.PI / 2f,
            ScrollUpButtonId => 0f,
            ScrollDownButtonId => MathF.PI,
            _ => null
        };
        if (rotation is null)
            return false;

        Rectangle source = MenuVisualMetrics.ArrowSource;
        float scale = Math.Min(
            MenuVisualMetrics.ArrowScale,
            Math.Min((button.bounds.Width - 8f) / source.Width,
                (button.bounds.Height - 8f) / source.Height));
        scale = Math.Max(1f, scale);

        batch.Draw(
            Game1.mouseCursors,
            button.bounds.Center.ToVector2(),
            source,
            Color.White,
            rotation.Value,
            new Vector2(source.Width / 2f, source.Height / 2f),
            scale,
            SpriteEffects.None,
            0.9f
        );
        return true;
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

    private sealed record ControlDefinition(string Key, Func<int, Rectangle, IConfigControl> Create);
}
