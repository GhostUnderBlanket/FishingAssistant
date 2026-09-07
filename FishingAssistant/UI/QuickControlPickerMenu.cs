using FishingAssistant.Configuration;
using FishingAssistant.HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using FishingAssistant.UI.Controls;

namespace FishingAssistant.UI;

/// <summary>Edits ordered quick actions in the owning configuration draft.</summary>
internal sealed class QuickControlPickerMenu : IClickableMenu
{
    private const float CardStateScale = 0.62f;
    private const int FirstActionId = 5000;
    private const int ScrollUpId = 6000;
    private const int ScrollDownId = 6001;
    private const int DoneId = 6002;

    private readonly ModConfig config;
    private readonly List<QuickControlAction> selectedActions;
    private readonly Func<string, string> translate;
    private readonly PickerAction[] allActions;
    private readonly List<ActionCard> visibleCards = [];
    private readonly List<int> visibleSeparatorYs = [];
    private readonly List<ClickableComponent> buttons = [];
    private IReadOnlyList<PickerAction> filteredActions;
    private IReadOnlyList<ActionRow> actionRows = [];
    private ItemPickerLayout layout = null!;
    private TextBox searchBox = null!;
    private Rectangle searchBounds;
    private string searchText = "";
    private string hoverText = "";
    private int topRow;

    public QuickControlPickerMenu(ModConfig config, Func<string, string> translate)
    {
        this.config = config;
        this.selectedActions = config.QuickControlActions;
        this.translate = translate;
        this.allActions = Enum.GetValues<QuickControlAction>()
            .Where(action => action != QuickControlAction.None)
            .Select(action => new PickerAction(action, this.GetActionName(action)))
            .ToArray();
        this.filteredActions = this.allActions;
        this.RebuildComponents();
        Game1.playSound("bigSelect");
    }

    private int MaximumTopRow => Math.Max(0, this.actionRows.Count - this.layout.Rows);

    public override bool areGamePadControlsImplemented() => true;

    public override bool showWithoutTransparencyIfOptionIsSet() => true;

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        this.DeselectSearch();
        this.RebuildComponents();
    }

    public override void snapToDefaultClickableComponent()
    {
        this.currentlySnappedComponent = this.visibleCards.FirstOrDefault()?.Component
            ?? this.buttons.First(button => button.myID == DoneId);
    }

    public override void setUpForGamePadMode()
    {
        this.snapToDefaultClickableComponent();
        this.snapCursorToCurrentSnappedComponent();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.upperRightCloseButton.containsPoint(x, y))
        {
            this.exitThisMenu();
            return;
        }

        if (this.searchBounds.Contains(x, y))
        {
            this.searchBox.Selected = true;
            return;
        }

        this.DeselectSearch();
        ActionCard? card = this.visibleCards.FirstOrDefault(item => item.Component.containsPoint(x, y));
        if (card is not null)
        {
            int order = this.selectedActions.IndexOf(card.Action.Value);
            if (order >= 0)
            {
                Rectangle previous = this.GetOrderButtonBounds(card.Component.bounds, movePrevious: true);
                Rectangle next = this.GetOrderButtonBounds(card.Component.bounds, movePrevious: false);
                if (previous.Contains(x, y))
                    this.MoveAction(card.Action.Value, -1);
                else if (next.Contains(x, y))
                    this.MoveAction(card.Action.Value, 1);
                else
                {
                    this.selectedActions.Remove(card.Action.Value);
                    Game1.playSound("trashcan");
                }
            }
            else if (this.selectedActions.Count < ModConfig.MaximumQuickControlSlots)
            {
                this.selectedActions.Add(card.Action.Value);
                Game1.playSound("coin");
            }
            else
            {
                Game1.playSound("cancel");
                return;
            }

            this.RebuildRowsAndCards();
            return;
        }

        ClickableComponent? button = this.buttons.FirstOrDefault(item => item.containsPoint(x, y));
        switch (button?.myID)
        {
            case ScrollUpId:
                this.Scroll(-1);
                break;
            case ScrollDownId:
                this.Scroll(1);
                break;
            case DoneId:
                this.exitThisMenu();
                break;
        }
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        if (this.searchBounds.Contains(x, y))
        {
            this.searchBox.Text = "";
            Game1.playSound("trashcan");
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
        if (this.searchBox.Selected)
        {
            if (key == Keys.Escape)
            {
                if (this.searchBox.Text.Length > 0)
                    this.searchBox.Text = "";
                else
                    this.DeselectSearch();
            }
            else if (key == Keys.Enter)
                this.DeselectSearch();
            return;
        }

        if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
        {
            this.exitThisMenu();
            return;
        }

        if (key is Keys.Enter or Keys.Space)
            this.ActivateSnappedComponent();
        else if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
            this.MoveVertical(-1);
        else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
            this.MoveVertical(1);
        else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
            this.applyMovementKey(3);
        else if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
            this.applyMovementKey(1);
    }

    public override void receiveGamePadButton(Buttons button)
    {
        if (ConfigurationMenuGamepadNavigation.IsDirectional(button))
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
                if (!this.MoveSnappedAction(-1))
                    this.Scroll(-this.layout.Rows);
                break;
            case Buttons.RightShoulder:
                if (!this.MoveSnappedAction(1))
                    this.Scroll(this.layout.Rows);
                break;
        }
    }

    public override void performHoverAction(int x, int y)
    {
        ActionCard? card = this.visibleCards.FirstOrDefault(item => item.Component.containsPoint(x, y));
        if (card is null)
        {
            this.hoverText = "";
            return;
        }

        int order = this.selectedActions.IndexOf(card.Action.Value);
        this.hoverText = order >= 0
            ? string.Format(this.translate("config.quick_picker.selected_help"), card.Action.DisplayName,
                order + 1, ConfigKeybind.FormatBindings(
                    this.config.GetQuickControlKeybind(order),
                    this.config.GetQuickControlOptionalKeybind(order)))
            : this.selectedActions.Count < ModConfig.MaximumQuickControlSlots
                ? string.Format(this.translate("config.quick_picker.available_help"), card.Action.DisplayName)
                : this.translate("config.quick_picker.full_help");
    }

    public override void update(GameTime time)
    {
        if (!string.Equals(this.searchText, this.searchBox.Text, StringComparison.Ordinal))
        {
            this.searchText = this.searchBox.Text;
            string query = this.searchText.Trim();
            this.filteredActions = query.Length == 0
                ? this.allActions
                : this.allActions.Where(action =>
                        action.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                        || action.Value.ToString().Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            this.topRow = 0;
            this.RebuildRowsAndCards();
        }

        base.update(time);
    }

    public override void draw(SpriteBatch batch)
    {
        batch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.78f);
        Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height,
            speaker: false, drawOnlyBox: true);
        this.DrawHeader(batch);
        this.searchBox.Draw(batch);
        if (!this.searchBox.Selected && this.searchBox.Text.Length == 0)
        {
            Utility.drawTextWithShadow(batch, this.translate("config.quick_picker.search"), Game1.smallFont,
                new Vector2(this.searchBox.X + 16, this.searchBox.Y + 12), Color.Gray);
        }

        Point mouse = new(Game1.getMouseX(), Game1.getMouseY());
        foreach (int separatorY in this.visibleSeparatorYs)
        {
            batch.Draw(Game1.staminaRect,
                new Rectangle(this.layout.ContentX,
                    separatorY - MenuVisualMetrics.ItemGroupSeparatorThickness / 2,
                    this.layout.ContentWidth,
                    MenuVisualMetrics.ItemGroupSeparatorThickness),
                MenuVisualMetrics.ItemGroupSeparatorColor);
        }
        foreach (ActionCard card in this.visibleCards)
            this.DrawCard(batch, card,
                card.Component.bounds.Contains(mouse) || this.currentlySnappedComponent == card.Component);
        foreach (ClickableComponent button in this.buttons)
            this.DrawButton(batch, button,
                button.bounds.Contains(mouse) || this.currentlySnappedComponent == button);

        if (this.filteredActions.Count == 0)
        {
            string empty = this.translate("config.quick_picker.no_results");
            Vector2 size = Game1.smallFont.MeasureString(empty);
            Utility.drawTextWithShadow(batch, empty, Game1.smallFont,
                new Vector2(this.layout.X + (this.layout.Width - size.X) / 2f,
                    this.layout.ContentTop + (this.layout.ContentBottom - this.layout.ContentTop - size.Y) / 2f),
                Game1.textColor);
        }

        base.draw(batch);
        MenuTooltip.Draw(batch, this.hoverText);
        this.drawMouse(batch);
    }

    protected override void cleanupBeforeExit()
    {
        this.DeselectSearch();
        base.cleanupBeforeExit();
    }

    private void RebuildComponents()
    {
        this.layout = ItemPickerLayout.Calculate(Game1.uiViewport.Width, Game1.uiViewport.Height);
        this.xPositionOnScreen = this.layout.X;
        this.yPositionOnScreen = this.layout.Y;
        this.width = this.layout.Width;
        this.height = this.layout.Height;
        this.initializeUpperRightCloseButton();

        this.searchBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null,
            Game1.smallFont, Game1.textColor)
        {
            X = this.layout.ContentX,
            Y = this.layout.Y + this.layout.HeaderHeight + 16,
            Width = this.layout.ContentWidth,
            Height = 48,
            Text = this.searchText
        };
        this.searchBounds = new Rectangle(this.searchBox.X, this.searchBox.Y,
            this.searchBox.Width, this.searchBox.Height);
        this.RebuildActionRows();
        this.topRow = Math.Clamp(this.topRow, 0, this.MaximumTopRow);
        this.RebuildVisibleCards();
        this.BuildButtons();
        this.BuildNavigation();
    }

    private void RebuildRowsAndCards()
    {
        this.RebuildActionRows();
        this.topRow = Math.Clamp(this.topRow, 0, this.MaximumTopRow);
        this.RebuildVisibleCards();
        this.BuildNavigation();
    }

    private void RebuildActionRows()
    {
        PickerAction[] selected = this.selectedActions
            .Select(value => this.filteredActions.FirstOrDefault(action => action.Value == value))
            .Where(action => action is not null)
            .Cast<PickerAction>()
            .ToArray();
        PickerAction[] available = this.filteredActions
            .Where(action => !this.selectedActions.Contains(action.Value))
            .ToArray();
        List<ActionRow> rows = [];
        this.AddRows(rows, selected, startsAvailableGroup: false);
        this.AddRows(rows, available, startsAvailableGroup: selected.Length > 0);
        this.actionRows = rows;
    }

    private void AddRows(List<ActionRow> rows, IReadOnlyList<PickerAction> actions, bool startsAvailableGroup)
    {
        for (int index = 0; index < actions.Count; index += this.layout.Columns)
        {
            rows.Add(new ActionRow(actions.Skip(index).Take(this.layout.Columns).ToArray(),
                startsAvailableGroup && index == 0));
        }
    }

    private void RebuildVisibleCards()
    {
        this.visibleCards.Clear();
        this.visibleSeparatorYs.Clear();
        (ActionRow Row, int VisibleRow)[] visibleRows = this.actionRows
            .Skip(this.topRow)
            .Take(this.layout.Rows)
            .Select((row, index) => (row, index))
            .ToArray();
        int separatorRow = Array.FindIndex(visibleRows,
            entry => entry.VisibleRow > 0 && entry.Row.StartsAvailableGroup);
        int separatorGap = MenuVisualMetrics.ItemGroupSeparatorThickness
            + MenuVisualMetrics.ItemGroupSeparatorVerticalPadding * 2;
        int separatorExtraGap = separatorRow >= 0 ? Math.Max(0, separatorGap - this.layout.Gap) : 0;
        int visibleCardHeight = visibleRows.Length == 0
            ? this.layout.CardHeight
            : Math.Min(this.layout.CardHeight, Math.Max(1,
                (this.layout.ContentBottom - this.layout.ContentTop
                    - this.layout.Gap * Math.Max(0, visibleRows.Length - 1)
                    - separatorExtraGap) / visibleRows.Length));
        int componentIndex = 0;
        int rowY = this.layout.ContentTop;
        foreach ((ActionRow row, int visibleRow) in visibleRows)
        {
            if (visibleRow == separatorRow)
            {
                int previousRowBottom = rowY - this.layout.Gap;
                this.visibleSeparatorYs.Add(previousRowBottom + separatorGap / 2);
                rowY += separatorExtraGap;
            }

            for (int column = 0; column < row.Actions.Count; column++)
            {
                PickerAction action = row.Actions[column];
                Rectangle bounds = new(
                    this.layout.ContentX + column * (this.layout.CardWidth + this.layout.Gap),
                    rowY, this.layout.CardWidth, visibleCardHeight);
                this.visibleCards.Add(new ActionCard(action,
                    new ClickableComponent(bounds, action.DisplayName) { myID = FirstActionId + componentIndex },
                    visibleRow, column));
                componentIndex++;
            }

            rowY += visibleCardHeight + this.layout.Gap;
        }
    }

    private void BuildButtons()
    {
        int buttonHeight = Math.Min(48, this.layout.FooterHeight - 8);
        int y = this.layout.Y + this.layout.Height - buttonHeight - 8;
        int doneWidth = Math.Min(220, Math.Max(100, this.layout.ContentWidth / 3));
        this.buttons.Clear();
        this.buttons.Add(new ClickableComponent(
            new Rectangle(this.layout.ContentX, y, buttonHeight, buttonHeight), "") { myID = ScrollUpId });
        this.buttons.Add(new ClickableComponent(
            new Rectangle(this.layout.ContentX + buttonHeight + 8, y, buttonHeight, buttonHeight), "")
            { myID = ScrollDownId });
        this.buttons.Add(new ClickableComponent(
            new Rectangle(this.layout.X + this.layout.Width - this.layout.Padding - doneWidth, y,
                doneWidth, buttonHeight), this.translate("config.action.done")) { myID = DoneId });
    }

    private void BuildNavigation()
    {
        ClickableComponent done = this.buttons.First(button => button.myID == DoneId);
        foreach (ActionCard actionCard in this.visibleCards)
        {
            ClickableComponent card = actionCard.Component;
            card.leftNeighborID = this.visibleCards.FirstOrDefault(other =>
                other.GridRow == actionCard.GridRow && other.GridColumn == actionCard.GridColumn - 1)?.Component.myID ?? -1;
            card.rightNeighborID = this.visibleCards.FirstOrDefault(other =>
                other.GridRow == actionCard.GridRow && other.GridColumn == actionCard.GridColumn + 1)?.Component.myID ?? -1;
            ActionCard? above = this.visibleCards.Where(other => other.GridRow < actionCard.GridRow)
                .OrderByDescending(other => other.GridRow)
                .ThenBy(other => Math.Abs(other.GridColumn - actionCard.GridColumn)).FirstOrDefault();
            ActionCard? below = this.visibleCards.Where(other => other.GridRow > actionCard.GridRow)
                .OrderBy(other => other.GridRow)
                .ThenBy(other => Math.Abs(other.GridColumn - actionCard.GridColumn)).FirstOrDefault();
            card.upNeighborID = above?.Component.myID ?? -1;
            card.downNeighborID = below?.Component.myID ?? DoneId;
        }

        done.upNeighborID = this.visibleCards.LastOrDefault()?.Component.myID ?? -1;
        this.allClickableComponents = this.visibleCards.Select(card => card.Component)
            .Concat(this.buttons).Append(this.upperRightCloseButton).ToList();
        if (this.currentlySnappedComponent is not null
            && !this.allClickableComponents.Contains(this.currentlySnappedComponent))
            this.snapToDefaultClickableComponent();
    }

    private void DrawHeader(SpriteBatch batch)
    {
        string count = $"{this.selectedActions.Count} / {ModConfig.MaximumQuickControlSlots}";
        Vector2 countSize = Game1.smallFont.MeasureString(count);
        Vector2 countPosition = new(
            this.layout.X + this.layout.Width - this.layout.Padding - countSize.X,
            this.layout.Y + 24);
        this.DrawHeaderPanel(batch, countPosition, countSize, Game1.smallFont.LineSpacing);
        Utility.drawTextWithShadow(batch, count, Game1.smallFont, countPosition, Game1.textColor);

        float titleWidth = Math.Max(1f,
            countPosition.X - this.layout.ContentX - MenuVisualMetrics.HeaderPanelHorizontalPadding * 2);
        string title = MenuText.Fit(this.translate("config.category.quickcontrols"), Game1.dialogueFont, titleWidth);
        Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
        Vector2 titlePosition = new(this.layout.ContentX, this.layout.Y + 16);
        this.DrawHeaderPanel(batch, titlePosition, titleSize, Game1.dialogueFont.LineSpacing);
        Utility.drawTextWithShadow(batch, title, Game1.dialogueFont, titlePosition, Game1.textColor);
    }

    private void DrawHeaderPanel(SpriteBatch batch, Vector2 position, Vector2 textSize, int lineSpacing)
    {
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            (int)position.X - MenuVisualMetrics.HeaderPanelHorizontalPadding,
            (int)position.Y - MenuVisualMetrics.HeaderPanelVerticalPadding,
            (int)Math.Ceiling(textSize.X) + MenuVisualMetrics.HeaderPanelHorizontalPadding * 2,
            lineSpacing + MenuVisualMetrics.HeaderPanelVerticalPadding * 2, Color.White);
    }

    private void DrawCard(SpriteBatch batch, ActionCard card, bool highlighted)
    {
        int order = this.selectedActions.IndexOf(card.Action.Value);
        bool selected = order >= 0;
        Rectangle bounds = card.Component.bounds;
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            bounds.X, bounds.Y, bounds.Width, bounds.Height, highlighted ? Color.Wheat : Color.White);
        if (selected)
        {
            batch.Draw(Game1.staminaRect,
                new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, bounds.Height - 10),
                Color.ForestGreen * 0.24f);
        }
        else if (this.selectedActions.Count >= ModConfig.MaximumQuickControlSlots)
        {
            batch.Draw(Game1.staminaRect,
                new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, bounds.Height - 10),
                MenuVisualMetrics.DisabledControlOverlay);
        }

        int iconSize = Math.Min(48, Math.Max(24, bounds.Height - 18));
        Rectangle iconBounds = new(bounds.X + 12, bounds.Center.Y - iconSize / 2, iconSize, iconSize);
        AutomationHudRenderer.DrawActionIcon(batch, iconBounds, card.Action.Value,
            selected || this.selectedActions.Count < ModConfig.MaximumQuickControlSlots ? Color.White : Color.Gray);
        int textLeft = bounds.X + Math.Min(68, bounds.Width / 3);
        int reserved = selected ? 32 : 12;
        string name = MenuText.Fit(card.Action.DisplayName, Game1.smallFont,
            bounds.Right - textLeft - reserved);
        Vector2 size = Game1.smallFont.MeasureString(name);
        float nameY = selected ? bounds.Center.Y - size.Y / 2f - 7f : bounds.Center.Y - size.Y / 2f;
        Utility.drawTextWithShadow(batch, name, Game1.smallFont,
            new Vector2(textLeft, nameY), Game1.textColor);
        if (selected)
        {
            string shortcut = ConfigKeybind.FormatBindings(
                this.config.GetQuickControlKeybind(order),
                this.config.GetQuickControlOptionalKeybind(order));
            string orderLabel = $"#{order + 1} / {shortcut}";
            batch.DrawString(Game1.smallFont, orderLabel,
                new Vector2(textLeft, bounds.Center.Y + 1), MenuVisualMetrics.ItemStateText,
                0f, Vector2.Zero, CardStateScale, SpriteEffects.None, 0.91f);
            this.DrawOrderButton(batch, this.GetOrderButtonBounds(bounds, movePrevious: true), true, order > 0);
            this.DrawOrderButton(batch, this.GetOrderButtonBounds(bounds, movePrevious: false), false,
                order < this.selectedActions.Count - 1);
        }
    }

    private Rectangle GetOrderButtonBounds(Rectangle cardBounds, bool movePrevious)
    {
        const int size = 24;
        return new Rectangle(cardBounds.Right - size - 7,
            movePrevious ? cardBounds.Y + 6 : cardBounds.Bottom - size - 6, size, size);
    }

    private void DrawOrderButton(SpriteBatch batch, Rectangle bounds, bool movePrevious, bool enabled)
    {
        Rectangle source = MenuVisualMetrics.ArrowSource;
        batch.Draw(Game1.mouseCursors, bounds.Center.ToVector2(), source,
            enabled ? Color.White : Color.Gray * 0.55f,
            movePrevious ? 0f : MathF.PI, new Vector2(source.Width / 2f, source.Height / 2f),
            Math.Min(MenuVisualMetrics.ArrowScale, 1.5f), SpriteEffects.None, 0.96f);
    }

    private bool MoveSnappedAction(int direction)
    {
        ActionCard? card = this.visibleCards.FirstOrDefault(item => item.Component == this.currentlySnappedComponent);
        return card is not null && this.MoveAction(card.Action.Value, direction);
    }

    private bool MoveAction(QuickControlAction action, int direction)
    {
        int index = this.selectedActions.IndexOf(action);
        int target = index + direction;
        if (index < 0 || target < 0 || target >= this.selectedActions.Count)
            return false;
        (this.selectedActions[index], this.selectedActions[target]) =
            (this.selectedActions[target], this.selectedActions[index]);
        this.RebuildRowsAndCards();
        Game1.playSound("shwip");
        return true;
    }

    private void DrawButton(SpriteBatch batch, ClickableComponent button, bool highlighted)
    {
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            button.bounds.X, button.bounds.Y, button.bounds.Width, button.bounds.Height,
            highlighted ? Color.Wheat : Color.White);
        if (button.myID is ScrollUpId or ScrollDownId)
        {
            Rectangle source = MenuVisualMetrics.ArrowSource;
            batch.Draw(Game1.mouseCursors, button.bounds.Center.ToVector2(), source, Color.White,
                button.myID == ScrollUpId ? 0f : MathF.PI,
                new Vector2(source.Width / 2f, source.Height / 2f), MenuVisualMetrics.ArrowScale,
                SpriteEffects.None, 0.9f);
            return;
        }

        string label = MenuText.Fit(button.name, Game1.smallFont, button.bounds.Width - 20);
        Vector2 size = Game1.smallFont.MeasureString(label);
        Utility.drawTextWithShadow(batch, label, Game1.smallFont,
            new Vector2(button.bounds.Center.X - size.X / 2f, button.bounds.Center.Y - size.Y / 2f),
            Game1.textColor);
    }

    private void ActivateSnappedComponent()
    {
        if (this.currentlySnappedComponent is null)
            this.snapToDefaultClickableComponent();
        if (this.currentlySnappedComponent is null)
            return;
        Point center = this.currentlySnappedComponent.bounds.Center;
        this.receiveLeftClick(center.X, center.Y);
    }

    private void MoveVertical(int direction)
    {
        ActionCard? selected = this.visibleCards.FirstOrDefault(card =>
            card.Component == this.currentlySnappedComponent);
        if (selected is not null && selected.GridRow == 0 && direction < 0 && this.topRow > 0)
        {
            int column = selected.GridColumn;
            this.Scroll(-1);
            this.currentlySnappedComponent = this.visibleCards.Where(card => card.GridRow == 0)
                .OrderBy(card => Math.Abs(card.GridColumn - column)).First().Component;
            this.snapCursorToCurrentSnappedComponent();
            return;
        }

        int lastVisibleRow = this.visibleCards.Count == 0 ? -1 : this.visibleCards.Max(card => card.GridRow);
        if (selected is not null && selected.GridRow == lastVisibleRow && direction > 0
            && this.topRow < this.MaximumTopRow)
        {
            int column = selected.GridColumn;
            this.Scroll(1);
            int newLastVisibleRow = this.visibleCards.Max(card => card.GridRow);
            this.currentlySnappedComponent = this.visibleCards.Where(card => card.GridRow == newLastVisibleRow)
                .OrderBy(card => Math.Abs(card.GridColumn - column)).First().Component;
            this.snapCursorToCurrentSnappedComponent();
            return;
        }

        this.applyMovementKey(direction < 0 ? 0 : 2);
    }

    private void Scroll(int rows)
    {
        int target = Math.Clamp(this.topRow + rows, 0, this.MaximumTopRow);
        if (target == this.topRow)
            return;
        this.topRow = target;
        this.RebuildVisibleCards();
        this.BuildNavigation();
        Game1.playSound("shwip");
    }

    private void DeselectSearch()
    {
        if (this.searchBox is not null)
            this.searchBox.Selected = false;
        Game1.closeTextEntry();
    }

    private string GetActionName(QuickControlAction action) =>
        this.translate($"config.value.{action.ToString().ToLowerInvariant()}");

    private sealed record PickerAction(QuickControlAction Value, string DisplayName);

    private sealed record ActionRow(IReadOnlyList<PickerAction> Actions, bool StartsAvailableGroup);

    private sealed record ActionCard(
        PickerAction Action,
        ClickableComponent Component,
        int GridRow,
        int GridColumn);
}
