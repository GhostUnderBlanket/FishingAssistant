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
    private readonly List<ClickableComponent> buttons = [];
    private IReadOnlyList<PickerItem> filteredItems;
    private ItemPickerLayout layout = null!;
    private TextBox searchBox = null!;
    private Rectangle searchBounds;
    private string searchText = "";
    private string hoverText = "";
    private int topRow;
    private bool positionedInitialSelection;

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

    private int MaximumTopRow => Math.Max(0,
        (int)Math.Ceiling(this.filteredItems.Count / (double)this.layout.Columns) - this.layout.Rows);

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
            case Buttons.DPadUp:
            case Buttons.LeftThumbstickUp:
                this.MoveVertical(-1);
                break;
            case Buttons.DPadDown:
            case Buttons.LeftThumbstickDown:
                this.MoveVertical(1);
                break;
            case Buttons.DPadLeft:
            case Buttons.LeftThumbstickLeft:
                this.applyMovementKey(3);
                break;
            case Buttons.DPadRight:
            case Buttons.LeftThumbstickRight:
                this.applyMovementKey(1);
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

        int searchWidth = Math.Min(460, this.layout.ContentWidth);
        this.searchBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null,
            Game1.smallFont, Game1.textColor)
        {
            X = this.layout.ContentX,
            Y = this.layout.Y + this.layout.HeaderHeight + 4,
            Width = searchWidth,
            Height = 48,
            Text = this.searchText
        };
        this.searchBounds = new Rectangle(this.searchBox.X, this.searchBox.Y, this.searchBox.Width, this.searchBox.Height);
        if (!this.positionedInitialSelection)
        {
            int selectedIndex = Array.FindIndex(this.allItems, item =>
                string.Equals(item.Id, this.getValue(), StringComparison.OrdinalIgnoreCase));
            if (selectedIndex >= 0)
                this.topRow = Math.Max(0, selectedIndex / this.layout.Columns - this.layout.Rows / 2);
            this.positionedInitialSelection = true;
        }
        this.topRow = Math.Clamp(this.topRow, 0, this.MaximumTopRow);
        this.RebuildVisibleCards();
        this.BuildButtons();
        this.BuildNavigation();
    }

    private void RebuildVisibleCards()
    {
        this.visibleCards.Clear();
        int start = this.topRow * this.layout.Columns;
        foreach ((PickerItem item, int index) in this.filteredItems
                     .Select((item, index) => (item, index))
                     .Skip(start)
                     .Take(this.layout.PageSize))
        {
            int visibleIndex = index - start;
            int column = visibleIndex % this.layout.Columns;
            int row = visibleIndex / this.layout.Columns;
            Rectangle bounds = new(
                this.layout.ContentX + column * (this.layout.CardWidth + this.layout.Gap),
                this.layout.ContentTop + row * (this.layout.CardHeight + this.layout.Gap),
                this.layout.CardWidth,
                this.layout.CardHeight);
            this.visibleCards.Add(new ItemCard(item,
                new ClickableComponent(bounds, item.DisplayName) { myID = FirstItemId + visibleIndex }));
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
            ClickableComponent card = this.visibleCards[index].Component;
            int column = index % this.layout.Columns;
            card.leftNeighborID = column == 0 ? -1 : this.visibleCards[index - 1].Component.myID;
            card.rightNeighborID = column == this.layout.Columns - 1 || index == this.visibleCards.Count - 1
                ? -1 : this.visibleCards[index + 1].Component.myID;
            card.upNeighborID = index < this.layout.Columns ? -1 : this.visibleCards[index - this.layout.Columns].Component.myID;
            card.downNeighborID = index + this.layout.Columns < this.visibleCards.Count
                ? this.visibleCards[index + this.layout.Columns].Component.myID : DoneId;
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
        string fitted = MenuText.Fit(this.title, Game1.dialogueFont, this.layout.ContentWidth);
        Utility.drawTextWithShadow(batch, fitted, Game1.dialogueFont,
            new Vector2(this.layout.ContentX, this.layout.Y + 20), Game1.textColor);
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
        Utility.drawTextWithShadow(batch, name, Game1.smallFont,
            new Vector2(textLeft, bounds.Center.Y - size.Y / 2f), Game1.textColor);
        if (selected)
        {
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
        int selectedIndex = this.visibleCards.FindIndex(card => card.Component == this.currentlySnappedComponent);
        bool crossingTop = selectedIndex >= 0 && selectedIndex < this.layout.Columns && direction < 0 && this.topRow > 0;
        bool crossingBottom = selectedIndex >= Math.Max(0, this.visibleCards.Count - this.layout.Columns)
            && direction > 0 && this.topRow < this.MaximumTopRow;
        if (crossingTop || crossingBottom)
        {
            int column = selectedIndex % this.layout.Columns;
            this.Scroll(direction);
            int targetIndex = direction < 0
                ? Math.Min(Math.Max(0, this.visibleCards.Count - this.layout.Columns) + column,
                    this.visibleCards.Count - 1)
                : Math.Min(column, this.visibleCards.Count - 1);
            this.currentlySnappedComponent = this.visibleCards[targetIndex].Component;
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

    private sealed record ItemCard(PickerItem Item, ClickableComponent Component);
}
