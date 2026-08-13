using FishingAssistant.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace FishingAssistant.UI;

internal sealed class SingleItemPickerMenu : IClickableMenu
{
    private const float CardStateScale = 0.62f;
    private const int FirstItemId = 3000;
    private const int ScrollUpId = 4000;
    private const int ScrollDownId = 4001;
    private const int DoneId = 4002;

    private readonly string title;
    private readonly PickerItem[] allItems;
    private readonly Func<string> getValue;
    private readonly Action<string> setValue;
    private readonly Func<string, string> translate;
    private readonly List<ItemCard> visibleCards = [];
    private readonly List<int> visibleSeparatorYs = [];
    private readonly List<ClickableComponent> buttons = [];
    private IReadOnlyList<PickerItem> filteredItems;
    private IReadOnlyList<ItemRow> itemRows = [];
    private ItemPickerLayout layout = null!;
    private TextBox searchBox = null!;
    private Rectangle searchBounds;
    private string searchText = "";
    private string hoverText = "";
    private int topRow;

    public SingleItemPickerMenu(
        string title,
        IReadOnlyList<ConfigItem> items,
        string sentinelId,
        string sentinelLabel,
        Func<string> getValue,
        Action<string> setValue,
        Func<string, string> translate)
    {
        this.title = title;
        this.allItems =
        [
            new PickerItem(sentinelId, sentinelLabel, null),
            .. items.Select(item => new PickerItem(item.QualifiedItemId, item.DisplayName, item))
        ];
        this.filteredItems = this.allItems;
        this.getValue = getValue;
        this.setValue = setValue;
        this.translate = translate;
        this.RebuildComponents();
        Game1.playSound("bigSelect");
    }

    private int MaximumTopRow => Math.Max(0, this.itemRows.Count - this.layout.Rows);

    public override bool areGamePadControlsImplemented() => true;

    public override bool showWithoutTransparencyIfOptionIsSet() => true;

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        this.DeselectSearch();
        this.RebuildComponents();
    }

    public override void snapToDefaultClickableComponent()
    {
        this.currentlySnappedComponent = this.visibleCards
            .FirstOrDefault(card => string.Equals(card.Item.Id, this.getValue(), StringComparison.OrdinalIgnoreCase))?
            .Component
            ?? this.visibleCards.FirstOrDefault()?.Component
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
        ItemCard? card = this.visibleCards.FirstOrDefault(item => item.Component.containsPoint(x, y));
        if (card is not null)
        {
            this.setValue(card.Item.Id);
            Game1.playSound("coin");
            this.RebuildItemRows();
            this.topRow = 0;
            this.RebuildVisibleCards();
            this.BuildNavigation();
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
        // Game1 sends a matching keyboard-direction event after this gamepad
        // event. receiveKeyPress owns directional navigation to avoid moving
        // the snapped component twice per controller press.
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
                this.Scroll(-this.layout.Rows);
                break;
            case Buttons.RightShoulder:
                this.Scroll(this.layout.Rows);
                break;
        }
    }

    public override void performHoverAction(int x, int y)
    {
        this.hoverText = this.visibleCards
            .FirstOrDefault(card => card.Component.containsPoint(x, y))?
            .Item.DisplayName ?? "";
    }

    public override void update(GameTime time)
    {
        if (!string.Equals(this.searchText, this.searchBox.Text, StringComparison.Ordinal))
        {
            this.searchText = this.searchBox.Text;
            string query = this.searchText.Trim();
            this.filteredItems = query.Length == 0
                ? this.allItems
                : this.allItems.Where(item =>
                        item.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                        || item.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            this.topRow = 0;
            this.RebuildItemRows();
            this.RebuildVisibleCards();
            this.BuildNavigation();
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
            Utility.drawTextWithShadow(batch, this.translate("config.junk_picker.search"), Game1.smallFont,
                new Vector2(this.searchBox.X + 16, this.searchBox.Y + 12), Color.Gray);
        }

        Point mouse = new(Game1.getMouseX(), Game1.getMouseY());
        foreach (int separatorY in this.visibleSeparatorYs)
        {
            batch.Draw(Game1.staminaRect,
                new Rectangle(
                    this.layout.ContentX,
                    separatorY - MenuVisualMetrics.ItemGroupSeparatorThickness / 2,
                    this.layout.ContentWidth,
                    MenuVisualMetrics.ItemGroupSeparatorThickness),
                MenuVisualMetrics.ItemGroupSeparatorColor);
        }
        foreach (ItemCard card in this.visibleCards)
            this.DrawCard(batch, card, card.Component.bounds.Contains(mouse) || this.currentlySnappedComponent == card.Component);
        foreach (ClickableComponent button in this.buttons)
            this.DrawButton(batch, button, button.bounds.Contains(mouse) || this.currentlySnappedComponent == button);

        if (this.filteredItems.Count == 0)
        {
            string empty = this.translate("config.junk_picker.empty");
            Vector2 size = Game1.smallFont.MeasureString(empty);
            Utility.drawTextWithShadow(batch, empty, Game1.smallFont,
                new Vector2(this.layout.X + (this.layout.Width - size.X) / 2f,
                    this.layout.ContentTop + (this.layout.ContentBottom - this.layout.ContentTop - size.Y) / 2f),
                Game1.textColor);
        }

        base.draw(batch);
        if (this.hoverText.Length > 0)
            drawHoverText(batch, this.hoverText, Game1.smallFont);
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

        int searchWidth = this.layout.ContentWidth;
        this.searchBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null,
            Game1.smallFont, Game1.textColor)
        {
            X = this.layout.ContentX,
            Y = this.layout.Y + this.layout.HeaderHeight + 16,
            Width = searchWidth,
            Height = 48,
            Text = this.searchText
        };
        this.searchBounds = new Rectangle(this.searchBox.X, this.searchBox.Y, this.searchBox.Width, this.searchBox.Height);
        this.RebuildItemRows();
        this.topRow = Math.Clamp(this.topRow, 0, this.MaximumTopRow);
        this.RebuildVisibleCards();
        this.BuildButtons();
        this.BuildNavigation();
    }

    private void RebuildVisibleCards()
    {
        this.visibleCards.Clear();
        this.visibleSeparatorYs.Clear();
        (ItemRow Row, int VisibleRow)[] visibleRows = this.itemRows
            .Skip(this.topRow)
            .Take(this.layout.Rows)
            .Select((row, index) => (row, index))
            .ToArray();
        int separatorRow = Array.FindIndex(visibleRows,
            entry => entry.VisibleRow > 0 && entry.Row.StartsNormalGroup);
        int separatorGap = MenuVisualMetrics.ItemGroupSeparatorThickness
            + MenuVisualMetrics.ItemGroupSeparatorVerticalPadding * 2;
        int separatorExtraGap = separatorRow >= 0
            ? Math.Max(0, separatorGap - this.layout.Gap)
            : 0;
        int visibleCardHeight = visibleRows.Length == 0
            ? this.layout.CardHeight
            : Math.Min(this.layout.CardHeight, Math.Max(1,
                (this.layout.ContentBottom - this.layout.ContentTop
                    - this.layout.Gap * Math.Max(0, visibleRows.Length - 1)
                    - separatorExtraGap) / visibleRows.Length));
        int componentIndex = 0;
        int rowY = this.layout.ContentTop;
        foreach ((ItemRow row, int visibleRow) in visibleRows)
        {
            if (visibleRow == separatorRow)
            {
                int previousRowBottom = rowY - this.layout.Gap;
                this.visibleSeparatorYs.Add(previousRowBottom + separatorGap / 2);
                rowY += separatorExtraGap;
            }

            for (int column = 0; column < row.Items.Count; column++)
            {
                PickerItem item = row.Items[column];
                Rectangle bounds = new(
                    this.layout.ContentX + column * (this.layout.CardWidth + this.layout.Gap),
                    rowY,
                    this.layout.CardWidth,
                    visibleCardHeight);
                this.visibleCards.Add(new ItemCard(
                    item,
                    new ClickableComponent(bounds, item.DisplayName) { myID = FirstItemId + componentIndex },
                    visibleRow,
                    column));
                componentIndex++;
            }

            rowY += visibleCardHeight + this.layout.Gap;
        }
    }

    private void RebuildItemRows()
    {
        string selectedId = this.getValue();
        PickerItem[] selected = this.filteredItems
            .Where(item => string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        PickerItem[] normal = this.filteredItems
            .Where(item => !string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        List<ItemRow> rows = [];
        this.AddRows(rows, selected, startsNormalGroup: false);
        this.AddRows(rows, normal, startsNormalGroup: selected.Length > 0);
        this.itemRows = rows;
    }

    private void AddRows(List<ItemRow> rows, IReadOnlyList<PickerItem> items, bool startsNormalGroup)
    {
        for (int index = 0; index < items.Count; index += this.layout.Columns)
        {
            rows.Add(new ItemRow(
                items.Skip(index).Take(this.layout.Columns).ToArray(),
                startsNormalGroup && index == 0));
        }
    }

    private void BuildButtons()
    {
        int buttonHeight = Math.Min(48, this.layout.FooterHeight - 8);
        int y = this.layout.Y + this.layout.Height - buttonHeight - 8;
        int doneWidth = Math.Min(220, Math.Max(100, this.layout.ContentWidth / 3));
        this.buttons.Clear();
        this.buttons.Add(new ClickableComponent(
            new Rectangle(this.layout.ContentX, y, buttonHeight, buttonHeight), "")
        { myID = ScrollUpId });
        this.buttons.Add(new ClickableComponent(
            new Rectangle(this.layout.ContentX + buttonHeight + 8, y, buttonHeight, buttonHeight), "")
        { myID = ScrollDownId });
        this.buttons.Add(new ClickableComponent(
            new Rectangle(this.layout.X + this.layout.Width - this.layout.Padding - doneWidth, y,
                doneWidth, buttonHeight), this.translate("config.action.done"))
        { myID = DoneId });
    }

    private void BuildNavigation()
    {
        ClickableComponent done = this.buttons.First(button => button.myID == DoneId);
        for (int index = 0; index < this.visibleCards.Count; index++)
        {
            ItemCard itemCard = this.visibleCards[index];
            ClickableComponent card = itemCard.Component;
            card.leftNeighborID = this.visibleCards
                .FirstOrDefault(other => other.GridRow == itemCard.GridRow && other.GridColumn == itemCard.GridColumn - 1)?
                .Component.myID ?? -1;
            card.rightNeighborID = this.visibleCards
                .FirstOrDefault(other => other.GridRow == itemCard.GridRow && other.GridColumn == itemCard.GridColumn + 1)?
                .Component.myID ?? -1;
            ItemCard? above = this.visibleCards
                .Where(other => other.GridRow < itemCard.GridRow)
                .OrderByDescending(other => other.GridRow)
                .ThenBy(other => Math.Abs(other.GridColumn - itemCard.GridColumn))
                .FirstOrDefault();
            ItemCard? below = this.visibleCards
                .Where(other => other.GridRow > itemCard.GridRow)
                .OrderBy(other => other.GridRow)
                .ThenBy(other => Math.Abs(other.GridColumn - itemCard.GridColumn))
                .FirstOrDefault();
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
        float titleWidth = Math.Max(1f,
            this.layout.ContentWidth - MenuVisualMetrics.HeaderPanelHorizontalPadding * 2);
        string fitted = MenuText.Fit(this.title, Game1.dialogueFont, titleWidth);
        Vector2 size = Game1.dialogueFont.MeasureString(fitted);
        Vector2 position = new(this.layout.ContentX, this.layout.Y + 16);
        this.DrawHeaderPanel(batch, position, size, Game1.dialogueFont.LineSpacing);
        Utility.drawTextWithShadow(batch, fitted, Game1.dialogueFont, position, Game1.textColor);
    }

    private void DrawHeaderPanel(SpriteBatch batch, Vector2 position, Vector2 textSize, int lineSpacing)
    {
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            (int)position.X - MenuVisualMetrics.HeaderPanelHorizontalPadding,
            (int)position.Y - MenuVisualMetrics.HeaderPanelVerticalPadding,
            (int)Math.Ceiling(textSize.X) + MenuVisualMetrics.HeaderPanelHorizontalPadding * 2,
            lineSpacing + MenuVisualMetrics.HeaderPanelVerticalPadding * 2,
            Color.White);
    }

    private void DrawCard(SpriteBatch batch, ItemCard card, bool highlighted)
    {
        bool selected = string.Equals(card.Item.Id, this.getValue(), StringComparison.OrdinalIgnoreCase);
        Rectangle bounds = card.Component.bounds;
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            bounds.X, bounds.Y, bounds.Width, bounds.Height,
            highlighted ? Color.Wheat : Color.White);
        if (selected)
            batch.Draw(Game1.staminaRect, new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, bounds.Height - 10),
                Color.ForestGreen * 0.24f);

        int textLeft = bounds.X + 14;
        if (card.Item.Item is ConfigItem item)
        {
            ParsedItemData? data = ItemRegistry.GetData(item.QualifiedItemId);
            if (data is not null)
            {
                Rectangle source = data.GetSourceRect();
                float scale = Math.Min(3f, Math.Max(1f, (bounds.Height - 18f) / source.Height));
                batch.Draw(data.GetTexture(), new Vector2(bounds.X + 12,
                        bounds.Center.Y - source.Height * scale / 2f), source, Color.White,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);
                textLeft = bounds.X + Math.Min(68, bounds.Width / 3);
            }
        }

        string name = MenuText.Fit(card.Item.DisplayName, Game1.smallFont,
            bounds.Right - textLeft - (selected ? 32 : 12));
        Vector2 size = Game1.smallFont.MeasureString(name);
        float nameY = selected
            ? bounds.Center.Y - size.Y / 2f - 7f
            : bounds.Center.Y - size.Y / 2f;
        Utility.drawTextWithShadow(batch, name, Game1.smallFont,
            new Vector2(textLeft, nameY), Game1.textColor);
        if (selected)
        {
            string selectedLabel = this.translate("config.item_picker.state_selected");
            float availableWidth = Math.Max(1f, bounds.Right - textLeft - 8);
            selectedLabel = MenuText.Fit(selectedLabel, Game1.smallFont, availableWidth / CardStateScale);
            batch.DrawString(Game1.smallFont, selectedLabel,
                new Vector2(textLeft, bounds.Center.Y + 1), MenuVisualMetrics.ItemStateText,
                0f, Vector2.Zero, CardStateScale, SpriteEffects.None, 0.91f);
            batch.Draw(Game1.mouseCursors, new Vector2(bounds.Right - 24, bounds.Y + 10),
                OptionsCheckbox.sourceRectChecked, Color.White, 0f, Vector2.Zero, 2f,
                SpriteEffects.None, 0.95f);
        }
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
            new Vector2(button.bounds.Center.X - size.X / 2f, button.bounds.Center.Y - size.Y / 2f), Game1.textColor);
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
        ItemCard? selected = this.visibleCards
            .FirstOrDefault(card => card.Component == this.currentlySnappedComponent);
        if (selected is not null && selected.GridRow == 0 && direction < 0 && this.topRow > 0)
        {
            int currentColumn = selected.GridColumn;
            this.Scroll(-1);
            this.currentlySnappedComponent = this.visibleCards
                .Where(card => card.GridRow == 0)
                .OrderBy(card => Math.Abs(card.GridColumn - currentColumn))
                .First().Component;
            this.snapCursorToCurrentSnappedComponent();
            return;
        }

        int lastVisibleRow = this.visibleCards.Count == 0 ? -1 : this.visibleCards.Max(card => card.GridRow);
        if (selected is not null && selected.GridRow == lastVisibleRow
            && direction > 0 && this.topRow < this.MaximumTopRow)
        {
            int currentColumn = selected.GridColumn;
            this.Scroll(1);
            int newLastVisibleRow = this.visibleCards.Max(card => card.GridRow);
            this.currentlySnappedComponent = this.visibleCards
                .Where(card => card.GridRow == newLastVisibleRow)
                .OrderBy(card => Math.Abs(card.GridColumn - currentColumn))
                .First().Component;
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

    private sealed record PickerItem(string Id, string DisplayName, ConfigItem? Item);

    private sealed record ItemRow(IReadOnlyList<PickerItem> Items, bool StartsNormalGroup);

    private sealed record ItemCard(
        PickerItem Item,
        ClickableComponent Component,
        int GridRow,
        int GridColumn);
}
