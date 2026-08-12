using FishingAssistant.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace FishingAssistant.UI;

internal sealed class JunkListMenu : IClickableMenu
{
    private const int FirstItemId = 1000;
    private const int ScrollUpId = 2000;
    private const int ScrollDownId = 2001;
    private const int DoneId = 2002;
    private const int EditJunkId = 2010;
    private const int EditIgnoreId = 2011;

    private readonly IReadOnlyList<ConfigItem> allItems;
    private readonly List<string> junkIds;
    private readonly List<string> ignoreIds;
    private readonly Func<string, string> translate;
    private readonly List<ItemCard> visibleCards = [];
    private readonly List<ClickableComponent> buttons = [];
    private IReadOnlyList<ConfigItem> filteredItems = [];
    private ItemPickerLayout layout = null!;
    private TextBox searchBox = null!;
    private Rectangle searchBounds;
    private string searchText = "";
    private string hoverText = "";
    private int topRow;
    private JunkListMode mode = JunkListMode.Junk;

    public JunkListMenu(
        List<string> junkIds,
        List<string> ignoreIds,
        IConfigItemSource itemSource,
        Func<string, string> translate)
    {
        this.junkIds = junkIds;
        this.ignoreIds = ignoreIds;
        this.translate = translate;
        this.allItems = itemSource.GetAllObjects();
        this.filteredItems = this.allItems;
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

    public override void setUpForGamePadMode()
    {
        this.snapToDefaultClickableComponent();
        this.snapCursorToCurrentSnappedComponent();
    }

    public override void snapToDefaultClickableComponent()
    {
        this.currentlySnappedComponent = this.visibleCards.FirstOrDefault()?.Component
            ?? this.buttons.First(button => button.myID == DoneId);
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
            JunkItemState state = JunkListSelection.Toggle(
                this.junkIds,
                this.ignoreIds,
                card.Item.QualifiedItemId,
                this.mode);
            Game1.playSound(state == JunkItemState.Normal ? "trashcan" : "coin");
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
            case EditJunkId:
                this.SetMode(JunkListMode.Junk);
                break;
            case EditIgnoreId:
                this.SetMode(JunkListMode.Ignore);
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
                this.SetMode(JunkListMode.Junk);
                break;
            case Buttons.RightShoulder:
                this.SetMode(JunkListMode.Ignore);
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
            this.filteredItems = JunkListSelection.Filter(this.allItems, this.searchText);
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
        {
            bool highlighted = button.bounds.Contains(mouse)
                || this.currentlySnappedComponent == button
                || button.myID == EditJunkId && this.mode == JunkListMode.Junk
                || button.myID == EditIgnoreId && this.mode == JunkListMode.Ignore;
            this.DrawButton(batch, button, highlighted);
        }

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

        int searchWidth = Math.Min(420, Math.Max(100, (this.layout.ContentWidth - 16) * 55 / 100));
        this.searchBox = new TextBox(
            Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
            null,
            Game1.smallFont,
            Game1.textColor)
        {
            X = this.layout.ContentX,
            Y = this.layout.Y + this.layout.HeaderHeight + 4,
            Width = searchWidth,
            Height = 48,
            Text = this.searchText
        };
        this.searchBounds = new Rectangle(this.searchBox.X, this.searchBox.Y, this.searchBox.Width, this.searchBox.Height);
        this.topRow = Math.Clamp(this.topRow, 0, this.MaximumTopRow);
        this.RebuildVisibleCards();
        this.BuildButtons();
        this.BuildNavigation();
    }

    private void RebuildVisibleCards()
    {
        this.visibleCards.Clear();
        int start = this.topRow * this.layout.Columns;
        IEnumerable<(ConfigItem Item, int Index)> items = this.filteredItems
            .Select((item, index) => (item, index))
            .Skip(start)
            .Take(this.layout.PageSize);

        foreach ((ConfigItem item, int index) in items)
        {
            int visibleIndex = index - start;
            int column = visibleIndex % this.layout.Columns;
            int row = visibleIndex / this.layout.Columns;
            Rectangle bounds = new(
                this.layout.ContentX + column * (this.layout.CardWidth + this.layout.Gap),
                this.layout.ContentTop + row * (this.layout.CardHeight + this.layout.Gap),
                this.layout.CardWidth,
                this.layout.CardHeight
            );
            this.visibleCards.Add(new ItemCard(
                item,
                new ClickableComponent(bounds, item.DisplayName) { myID = FirstItemId + visibleIndex }
            ));
        }
    }

    private void BuildButtons()
    {
        int buttonHeight = Math.Min(48, this.layout.FooterHeight - 8);
        int y = this.layout.Y + this.layout.Height - buttonHeight - 8;
        int arrowSize = buttonHeight;
        int doneWidth = Math.Min(220, Math.Max(100, this.layout.ContentWidth / 3));
        int modeGap = 8;
        int modeLeft = this.searchBounds.Right + modeGap;
        int modeWidth = Math.Max(1, (this.layout.ContentX + this.layout.ContentWidth - modeLeft - modeGap) / 2);
        this.buttons.Clear();
        this.buttons.Add(new ClickableComponent(
            new Rectangle(modeLeft, this.searchBounds.Y, modeWidth, this.searchBounds.Height),
            this.translate("config.junk_picker.mode_junk"))
        { myID = EditJunkId });
        this.buttons.Add(new ClickableComponent(
            new Rectangle(modeLeft + modeWidth + modeGap, this.searchBounds.Y, modeWidth, this.searchBounds.Height),
            this.translate("config.junk_picker.mode_ignore"))
        { myID = EditIgnoreId });
        this.buttons.Add(new ClickableComponent(
            new Rectangle(this.layout.ContentX, y, arrowSize, buttonHeight), "")
        { myID = ScrollUpId });
        this.buttons.Add(new ClickableComponent(
            new Rectangle(this.layout.ContentX + arrowSize + 8, y, arrowSize, buttonHeight), "")
        { myID = ScrollDownId });
        this.buttons.Add(new ClickableComponent(
            new Rectangle(this.layout.X + this.layout.Width - this.layout.Padding - doneWidth, y, doneWidth, buttonHeight),
            this.translate("config.action.done"))
        { myID = DoneId });
    }

    private void BuildNavigation()
    {
        ClickableComponent done = this.buttons.First(button => button.myID == DoneId);
        ClickableComponent editJunk = this.buttons.First(button => button.myID == EditJunkId);
        ClickableComponent editIgnore = this.buttons.First(button => button.myID == EditIgnoreId);
        editJunk.rightNeighborID = EditIgnoreId;
        editIgnore.leftNeighborID = EditJunkId;
        editJunk.downNeighborID = this.visibleCards.FirstOrDefault()?.Component.myID ?? DoneId;
        editIgnore.downNeighborID = this.visibleCards.FirstOrDefault()?.Component.myID ?? DoneId;
        for (int index = 0; index < this.visibleCards.Count; index++)
        {
            ClickableComponent card = this.visibleCards[index].Component;
            int column = index % this.layout.Columns;
            card.leftNeighborID = column == 0 ? -1 : this.visibleCards[index - 1].Component.myID;
            card.rightNeighborID = column == this.layout.Columns - 1 || index == this.visibleCards.Count - 1
                ? -1
                : this.visibleCards[index + 1].Component.myID;
            card.upNeighborID = index < this.layout.Columns
                ? (index < Math.Ceiling(this.layout.Columns / 2d) ? EditJunkId : EditIgnoreId)
                : this.visibleCards[index - this.layout.Columns].Component.myID;
            card.downNeighborID = index + this.layout.Columns < this.visibleCards.Count
                ? this.visibleCards[index + this.layout.Columns].Component.myID
                : DoneId;
        }

        done.upNeighborID = this.visibleCards.LastOrDefault()?.Component.myID ?? -1;
        this.allClickableComponents = this.visibleCards.Select(card => card.Component)
            .Concat(this.buttons)
            .Append(this.upperRightCloseButton)
            .ToList();
        if (this.currentlySnappedComponent is not null
            && !this.allClickableComponents.Contains(this.currentlySnappedComponent))
            this.snapToDefaultClickableComponent();
    }

    private void DrawHeader(SpriteBatch batch)
    {
        string title = MenuText.Fit(this.translate("config.junk_picker.title"), Game1.dialogueFont,
            this.layout.Width - this.layout.Padding * 2 - 220);
        Utility.drawTextWithShadow(batch, title, Game1.dialogueFont,
            new Vector2(this.layout.ContentX, this.layout.Y + 20), Game1.textColor);

        string count = string.Format(this.translate("config.junk_picker.selected"),
            this.junkIds.Count, this.ignoreIds.Count);
        Vector2 countSize = Game1.smallFont.MeasureString(count);
        Utility.drawTextWithShadow(batch, count, Game1.smallFont,
            new Vector2(this.layout.X + this.layout.Width - this.layout.Padding - countSize.X,
                this.layout.Y + 28), Game1.textColor);
    }

    private void DrawCard(SpriteBatch batch, ItemCard card, bool highlighted)
    {
        JunkItemState state = JunkListSelection.GetState(
            this.junkIds,
            this.ignoreIds,
            card.Item.QualifiedItemId);
        Rectangle bounds = card.Component.bounds;
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            bounds.X, bounds.Y, bounds.Width, bounds.Height,
            highlighted ? Color.Wheat : Color.White);
        if (state != JunkItemState.Normal)
            batch.Draw(Game1.staminaRect, new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, bounds.Height - 10),
                (state == JunkItemState.Junk ? Color.IndianRed : Color.ForestGreen) * 0.24f);

        ParsedItemData? data = ItemRegistry.GetData(card.Item.QualifiedItemId);
        if (data is not null)
        {
            Texture2D texture = data.GetTexture();
            Rectangle source = data.GetSourceRect();
            float scale = Math.Min(3f, Math.Max(1f, (bounds.Height - 18f) / source.Height));
            Vector2 iconPosition = new(bounds.X + 12, bounds.Center.Y - source.Height * scale / 2f);
            batch.Draw(texture, iconPosition, source, Color.White, 0f, Vector2.Zero, scale,
                SpriteEffects.None, 0.9f);
        }

        int textLeft = bounds.X + Math.Min(68, bounds.Width / 3);
        int reserved = state == JunkItemState.Normal ? 10 : 30;
        string name = MenuText.Fit(card.Item.DisplayName, Game1.smallFont, bounds.Right - textLeft - reserved);
        Vector2 size = Game1.smallFont.MeasureString(name);
        float nameY = state == JunkItemState.Normal
            ? bounds.Center.Y - size.Y / 2f
            : bounds.Center.Y - size.Y;
        Utility.drawTextWithShadow(batch, name, Game1.smallFont,
            new Vector2(textLeft, nameY), Game1.textColor);

        if (state != JunkItemState.Normal)
        {
            string stateLabel = this.translate(state == JunkItemState.Junk
                ? "config.junk_picker.state_junk"
                : "config.junk_picker.state_ignore");
            stateLabel = MenuText.Fit(stateLabel, Game1.tinyFont, bounds.Right - textLeft - 8);
            batch.DrawString(Game1.tinyFont, stateLabel,
                new Vector2(textLeft, bounds.Center.Y + 2),
                state == JunkItemState.Junk ? Color.DarkRed : Color.DarkGreen);
            batch.Draw(Game1.mouseCursors, new Vector2(bounds.Right - 24, bounds.Y + 10),
                OptionsCheckbox.sourceRectChecked, Color.White, 0f, Vector2.Zero, 2f,
                SpriteEffects.None, 0.95f);
        }
    }

    private void DrawButton(SpriteBatch batch, ClickableComponent button, bool highlighted)
    {
        Color tint = button.myID == EditJunkId && this.mode == JunkListMode.Junk
            ? Color.LightCoral
            : button.myID == EditIgnoreId && this.mode == JunkListMode.Ignore
                ? Color.LightGreen
                : highlighted ? Color.Wheat : Color.White;
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            button.bounds.X, button.bounds.Y, button.bounds.Width, button.bounds.Height,
            tint);
        if (button.myID is ScrollUpId or ScrollDownId)
        {
            Rectangle source = MenuVisualMetrics.ArrowSource;
            float rotation = button.myID == ScrollUpId ? 0f : MathF.PI;
            batch.Draw(Game1.mouseCursors, button.bounds.Center.ToVector2(), source, Color.White,
                rotation, new Vector2(source.Width / 2f, source.Height / 2f), MenuVisualMetrics.ArrowScale,
                SpriteEffects.None, 0.9f);
            return;
        }

        string label = MenuText.Fit(button.name, Game1.smallFont, button.bounds.Width - 12);
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
        int selectedIndex = this.visibleCards.FindIndex(card => card.Component == this.currentlySnappedComponent);
        if (selectedIndex >= 0 && selectedIndex < this.layout.Columns
            && direction < 0 && this.topRow > 0)
        {
            int currentColumn = selectedIndex % this.layout.Columns;
            this.Scroll(-1);
            int targetIndex = Math.Min(currentColumn, this.visibleCards.Count - 1);
            this.currentlySnappedComponent = this.visibleCards[targetIndex].Component;
            this.snapCursorToCurrentSnappedComponent();
            return;
        }

        if (selectedIndex >= Math.Max(0, this.visibleCards.Count - this.layout.Columns)
            && direction > 0 && this.topRow < this.MaximumTopRow)
        {
            int currentColumn = selectedIndex % this.layout.Columns;
            this.Scroll(1);
            this.currentlySnappedComponent = this.visibleCards[Math.Min(currentColumn, this.visibleCards.Count - 1)].Component;
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

    private void SetMode(JunkListMode mode)
    {
        if (this.mode == mode)
            return;

        this.mode = mode;
        Game1.playSound("smallSelect");
    }

    private void DeselectSearch()
    {
        if (this.searchBox is not null)
            this.searchBox.Selected = false;
        Game1.closeTextEntry();
    }

    private sealed record ItemCard(ConfigItem Item, ClickableComponent Component);
}
