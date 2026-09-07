using FishingAssistant.Configuration;
using FishingAssistant.Fishing;
using FishingAssistant.Runtime;
using FishingAssistant.UI;
using FishingAssistant.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace FishingAssistant.HUD;

internal sealed class AutomationHudRenderer(
    Func<string, string> translate,
    ActivityLogService activityLog)
{
    private const string FallbackRodId = "(T)AdvancedIridiumRod";
    private const int BarHeight = 64;
    private const int PanelPadding = 4;
    private const int ButtonGap = 8;
    private const int SettingsButtonWidth = 48;
    private const int ExpandButtonWidth = 52;
    private const int MinimumLogWidth = 180;
    private const int MaximumLogWidth = 420;
    private const int ActivityHorizontalPadding = 14;
    private const int ExpandedRowHeight = 38;
    private const int ExpandedRows = 6;
    private const int ExpandedTopPadding = 12;
    private const int ExpandedHeaderHeight = 36;
    private const int ExpandedRowsTopPadding = 8;
    private const int ExpandedBottomPadding = 14;
    private static readonly Rectangle TreasureHunterSource = new(137, 412, 10, 11);
    private static readonly Rectangle SettingsSource = new(154, 154, 20, 20);
    private readonly PerScreen<HudScreenState> screens = new(() => new HudScreenState());

    public void Draw(SpriteBatch batch, AutomationSession session, ModConfig config)
    {
        bool isFishingMinigame = Game1.currentMinigame is FishingGame;
        bool isSupportedFestivalFishing = FestivalFishingContext.IsSupportedFishingActivity;
        bool hasFishingRod = Game1.player.CurrentTool is StardewValley.Tools.FishingRod;
        bool isFishingActive = Game1.activeClickableMenu is BobberBar
            || isFishingMinigame
            || isSupportedFestivalFishing;
        bool hasBlockingMenu = Game1.activeClickableMenu is not null
            && Game1.activeClickableMenu is not BobberBar;
        if (!AutomationHudVisibilityPolicy.ShouldDraw(new(
                config.HudVisibility,
                Game1.displayHUD,
                hasBlockingMenu,
                Game1.eventUp,
                Game1.isFestival(),
                Game1.currentMinigame is not null && !isFishingMinigame,
                isSupportedFestivalFishing,
                hasFishingRod,
                isFishingActive)))
        {
            this.screens.Value.ClearHitBounds();
            return;
        }

        Toolbar? toolbar = Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
        bool toolbarAtTop = IsToolbarAtTop();
        float opacity = Game1.isFestival() || toolbar is null
            ? 1f
            : Math.Clamp(toolbar.transparency, 0.35f, 1f);

        // Keep the original rod-and-badge status HUD beside the toolbar. The
        // Quick Controls bar is an additional surface, not its replacement.
        this.DrawCompactStatus(batch, session, config, toolbar, opacity, toolbarAtTop,
            isFishingMinigame);

        if (!config.ShowAssistantBar || isFishingMinigame || Game1.isFestival())
        {
            this.screens.Value.ClearHitBounds();
            return;
        }

        HudScreenState screen = this.screens.Value;
        int visibleActionCount = config.QuickControlActions.Count;
        Rectangle barBounds = this.PlaceBar(toolbar, toolbarAtTop, visibleActionCount);
        bool pointerEnabled = Game1.activeClickableMenu is null;
        Point mouse = pointerEnabled
            ? new Point(Game1.getMouseX(), Game1.getMouseY())
            : new Point(-1, -1);
        if (barBounds.Contains(mouse) || screen.ExpandedBounds.Contains(mouse))
            opacity = 1f;

        this.DrawControlBar(batch, screen, barBounds, session, config, opacity, toolbarAtTop, mouse);
    }

    public QuickControlHudCommand HitTest(int x, int y)
    {
        HudScreenState screen = this.screens.Value;
        Point point = new(x, y);
        foreach ((QuickControlAction action, Rectangle bounds) in screen.ActionBounds)
        {
            if (bounds.Contains(point))
                return new QuickControlHudCommand(QuickControlHudCommandType.Action, action);
        }

        if (screen.SettingsBounds.Contains(point))
            return new QuickControlHudCommand(QuickControlHudCommandType.OpenSettings);
        if (screen.ExpandBounds.Contains(point))
            return new QuickControlHudCommand(QuickControlHudCommandType.ToggleLog);
        if (screen.ClearBounds.Contains(point))
            return new QuickControlHudCommand(QuickControlHudCommandType.ClearLog);
        return QuickControlHudCommand.None;
    }

    public void ToggleLogCurrent()
    {
        HudScreenState screen = this.screens.Value;
        screen.IsExpanded = !screen.IsExpanded;
        if (screen.IsExpanded)
            screen.ScrollOffset = 0;
    }

    public bool TryScrollLogCurrent(int x, int y, int direction)
    {
        HudScreenState screen = this.screens.Value;
        if (!screen.IsExpanded || !screen.ExpandedBounds.Contains(x, y) || direction == 0)
            return false;

        int maximumOffset = Math.Max(0, activityLog.Current.Count - screen.VisibleRowCount);
        screen.ScrollOffset = Math.Clamp(screen.ScrollOffset + direction * 3, 0, maximumOffset);
        return true;
    }

    public void ResetCurrent() => this.screens.Value.Reset();

    public void ResetAll() => this.screens.ResetAllScreens();

    private void DrawControlBar(
        SpriteBatch batch,
        HudScreenState screen,
        Rectangle barBounds,
        AutomationSession session,
        ModConfig config,
        float opacity,
        bool toolbarAtTop,
        Point mouse)
    {
        int actionCount = config.QuickControlActions.Count;
        int reserved = SettingsButtonWidth + MinimumLogWidth
            + ButtonGap * (actionCount + 1);
        int availableForActions = Math.Max(0, barBounds.Width - reserved);
        int actionSize = actionCount == 0
            ? 0
            : Math.Clamp(availableForActions / actionCount, 32, 52);
        int islandHeight = barBounds.Height - PanelPadding * 2;
        int x = barBounds.X;

        screen.ActionBounds.Clear();
        for (int index = 0; index < actionCount; index++)
        {
            QuickControlAction action = config.QuickControlActions[index];
            Rectangle bounds = new(x, barBounds.Y + PanelPadding, actionSize, islandHeight);
            screen.ActionBounds.Add((action, bounds));
            this.DrawActionButton(batch, bounds, action, session, config, bounds.Contains(mouse), opacity,
                index + 1);
            x += actionSize + ButtonGap;
        }

        int settingsX = barBounds.Right - SettingsButtonWidth;
        Rectangle activityBounds = new(
            x,
            barBounds.Y + PanelPadding,
            Math.Max(1, settingsX - ButtonGap - x),
            islandHeight);
        screen.ExpandBounds = new Rectangle(
            Math.Max(activityBounds.Left, activityBounds.Right - ExpandButtonWidth),
            activityBounds.Y,
            Math.Min(ExpandButtonWidth, activityBounds.Width),
            activityBounds.Height);
        screen.SettingsBounds = new Rectangle(settingsX, activityBounds.Y, SettingsButtonWidth, activityBounds.Height);
        screen.ClearBounds = Rectangle.Empty;
        screen.BarBounds = barBounds;

        if (screen.IsExpanded)
            this.DrawExpandedLog(batch, screen, activityBounds, toolbarAtTop, mouse);
        else
        {
            screen.ExpandedBounds = Rectangle.Empty;
            this.DrawLatestActivity(batch, activityBounds, screen.ExpandBounds, opacity);
            this.DrawExpandButton(batch, screen.ExpandBounds, expanded: false, opacity);
        }

        this.DrawSettingsButton(batch, screen.SettingsBounds, screen.SettingsBounds.Contains(mouse), opacity);

        this.DrawHoverText(batch, screen, mouse, config);
    }

    private void DrawActionButton(
        SpriteBatch batch,
        Rectangle bounds,
        QuickControlAction action,
        AutomationSession session,
        ModConfig config,
        bool highlighted,
        float opacity,
        int shortcutSlot)
    {
        bool active = IsActionActive(action, session, config);
        Color tint = highlighted ? Color.Wheat : Color.White;
        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            bounds.X, bounds.Y, bounds.Width, bounds.Height, tint * opacity);

        Rectangle iconBounds = new(bounds.Center.X - 16, bounds.Center.Y - 16, 32, 32);
        Color iconTint = (active ? Color.White : Color.Gray * 0.65f) * opacity;
        DrawActionIcon(batch, iconBounds, action, iconTint);
        string slotLabel = shortcutSlot.ToString();
        const float labelScale = 0.55f;
        Vector2 labelSize = Game1.smallFont.MeasureString(slotLabel) * labelScale;
        batch.DrawString(Game1.smallFont, slotLabel,
            new Vector2(bounds.Right - labelSize.X - 6, bounds.Bottom - labelSize.Y - 3),
            Game1.textColor * opacity, 0f, Vector2.Zero, labelScale,
            SpriteEffects.None, 0.97f);
    }

    internal static void DrawActionIcon(SpriteBatch batch, Rectangle bounds, QuickControlAction action, Color color)
    {
        if (action == QuickControlAction.ToggleAutomation)
        {
            string rodId = Game1.player.CurrentTool is StardewValley.Tools.FishingRod rod
                ? rod.QualifiedItemId
                : FallbackRodId;
            DrawItem(batch, bounds, rodId, color);
            return;
        }

        if (action == QuickControlAction.ToggleTreasureTargeting)
        {
            batch.Draw(Game1.mouseCursors, bounds, TreasureHunterSource, color);
            return;
        }

        string itemId = action switch
        {
            QuickControlAction.ToggleAutoLootTreasure => "(O)166",
            QuickControlAction.ToggleAutoEatFood => "(O)194",
            QuickControlAction.ToggleJunkDisposal => "(O)168",
            QuickControlAction.ToggleFishPreview => "(O)SonarBobber",
            QuickControlAction.ToggleSkipMinigame => "(O)685",
            _ => "(O)168"
        };
        DrawItem(batch, bounds, itemId, color);
    }

    private void DrawLatestActivity(
        SpriteBatch batch,
        Rectangle bounds,
        Rectangle expandBounds,
        float opacity)
    {
        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White * opacity);
        ActivityLogEntry? latest = activityLog.Current.LastOrDefault();
        string message = latest is null
            ? translate("hud.quick_controls.empty_log")
            : FormatEntry(latest);
        int textLeft = bounds.X + ActivityHorizontalPadding + 34;
        if (latest?.ItemId is not null)
        {
            DrawItem(batch, new Rectangle(bounds.X + ActivityHorizontalPadding, bounds.Center.Y - 15, 30, 30), latest.ItemId,
                Color.White * opacity);
        }
        else
            DrawSeverityIcon(batch, new Rectangle(bounds.X + ActivityHorizontalPadding + 2, bounds.Center.Y - 13, 26, 26),
                latest?.Severity ?? ActivityLogSeverity.Information, opacity);

        string fitted = MenuText.Fit(message, Game1.smallFont,
            Math.Max(1, expandBounds.Left - textLeft - ActivityHorizontalPadding) / 0.72f);
        batch.DrawString(Game1.smallFont, fitted,
            new Vector2(textLeft, bounds.Center.Y - Game1.smallFont.LineSpacing * 0.36f),
            GetSeverityColor(latest?.Severity ?? ActivityLogSeverity.Information) * opacity,
            0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0.95f);
    }

    private void DrawExpandedLog(
        SpriteBatch batch,
        HudScreenState screen,
        Rectangle activityBounds,
        bool toolbarAtTop,
        Point mouse)
    {
        IReadOnlyList<ActivityLogEntry> allEntries = activityLog.Current;
        int fixedHeight = ExpandedTopPadding + ExpandedHeaderHeight
            + ExpandedRowsTopPadding + ExpandedBottomPadding;
        int availableHeight = toolbarAtTop
            ? Game1.uiViewport.Height - activityBounds.Top - 8
            : activityBounds.Bottom - 8;
        int maximumRows = Math.Max(1, (availableHeight - fixedHeight) / ExpandedRowHeight);
        int rowCount = Math.Min(Math.Max(1, allEntries.Count),
            Math.Min(ExpandedRows, maximumRows));
        screen.VisibleRowCount = rowCount;
        if (allEntries.Count > screen.LastEntryCount && screen.ScrollOffset > 0)
            screen.ScrollOffset += allEntries.Count - screen.LastEntryCount;
        screen.LastEntryCount = allEntries.Count;
        int maximumOffset = Math.Max(0, allEntries.Count - rowCount);
        screen.ScrollOffset = Math.Clamp(screen.ScrollOffset, 0, maximumOffset);
        int endIndex = allEntries.Count - screen.ScrollOffset;
        int startIndex = Math.Max(0, endIndex - rowCount);
        ActivityLogEntry[] entries = allEntries.Skip(startIndex).Take(endIndex - startIndex).ToArray();
        int height = fixedHeight + rowCount * ExpandedRowHeight;
        int y = toolbarAtTop ? activityBounds.Top : activityBounds.Bottom - height;
        Rectangle panel = new(activityBounds.X, y, activityBounds.Width, height);
        screen.ExpandedBounds = panel;

        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            panel.X, panel.Y, panel.Width, panel.Height, Color.White);
        int headerY = panel.Y + ExpandedTopPadding;
        int headerRight = panel.Right - ActivityHorizontalPadding;
        Rectangle headerBand = new(panel.X, headerY, panel.Width, ExpandedHeaderHeight);
        if (headerBand.Intersects(screen.ExpandBounds))
            headerRight = screen.ExpandBounds.Left - ButtonGap;
        screen.ClearBounds = new Rectangle(
            headerRight - 80,
            headerY + 1,
            80,
            ExpandedHeaderHeight - 2);

        int titleRight = screen.ClearBounds.Left - ButtonGap;
        string range = entries.Length == 0
            ? "0/0"
            : $"{startIndex + 1}-{endIndex}/{allEntries.Count}";
        string title = MenuText.Fit(
            $"{translate("hud.quick_controls.activity_log")}  {range}",
            Game1.smallFont,
            Math.Max(1, titleRight - panel.X - ActivityHorizontalPadding) / 0.8f);
        Vector2 titleSize = Game1.smallFont.MeasureString(title) * 0.8f;
        batch.DrawString(Game1.smallFont, title,
            new Vector2(panel.X + ActivityHorizontalPadding,
                headerY + (ExpandedHeaderHeight - titleSize.Y) / 2f),
            Game1.textColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0.95f);

        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            screen.ClearBounds.X, screen.ClearBounds.Y, screen.ClearBounds.Width,
            screen.ClearBounds.Height, screen.ClearBounds.Contains(mouse) ? Color.Wheat : Color.White);
        string clear = translate("hud.quick_controls.clear");
        Vector2 clearSize = Game1.smallFont.MeasureString(clear) * 0.65f;
        batch.DrawString(Game1.smallFont, clear,
            new Vector2(screen.ClearBounds.Center.X - clearSize.X / 2,
                screen.ClearBounds.Center.Y - clearSize.Y / 2),
            Game1.textColor, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0.95f);

        int rowY = headerY + ExpandedHeaderHeight + ExpandedRowsTopPadding;
        if (entries.Length == 0)
        {
            Utility.drawTextWithShadow(batch, translate("hud.quick_controls.empty_log"),
                Game1.smallFont,
                new Vector2(panel.X + ActivityHorizontalPadding, rowY + 7),
                Game1.textColor,
                scale: 0.72f);
            this.DrawExpandButton(batch, screen.ExpandBounds, expanded: true, opacity: 1f);
            return;
        }

        foreach (ActivityLogEntry entry in entries)
        {
            if (entry.ItemId is not null)
                DrawItem(batch,
                    new Rectangle(panel.X + ActivityHorizontalPadding, rowY + 4, 30, 30),
                    entry.ItemId,
                    Color.White);
            else
                DrawSeverityIcon(batch,
                    new Rectangle(panel.X + ActivityHorizontalPadding + 2, rowY + 6, 26, 26),
                    entry.Severity, 1f);
            string time = entry.TimeOfDay > 0 ? Game1.getTimeOfDayString(entry.TimeOfDay) : "--";
            string message = $"{time}  {FormatEntry(entry)}";
            int textX = panel.X + ActivityHorizontalPadding + 38;
            int textRight = new Rectangle(panel.X, rowY, panel.Width, ExpandedRowHeight)
                .Intersects(screen.ExpandBounds)
                    ? screen.ExpandBounds.Left - ButtonGap
                    : panel.Right - ActivityHorizontalPadding;
            string fitted = MenuText.Fit(message, Game1.smallFont,
                Math.Max(1, textRight - textX) / 0.7f);
            batch.DrawString(Game1.smallFont, fitted, new Vector2(textX, rowY + 7),
                GetSeverityColor(entry.Severity), 0f, Vector2.Zero, 0.7f,
                SpriteEffects.None, 0.95f);
            rowY += ExpandedRowHeight;
        }

        this.DrawExpandButton(batch, screen.ExpandBounds, expanded: true, opacity: 1f);
    }

    private void DrawExpandButton(SpriteBatch batch, Rectangle bounds, bool expanded, float opacity)
    {
        Rectangle source = MenuVisualMetrics.ArrowSource;
        float rotation = expanded ? MathF.PI : 0f;
        batch.Draw(Game1.mouseCursors, bounds.Center.ToVector2(), source, Color.White * opacity,
            rotation, new Vector2(source.Width / 2f, source.Height / 2f), 1.7f,
            SpriteEffects.None, 0.95f);
    }

    private void DrawSettingsButton(SpriteBatch batch, Rectangle bounds, bool highlighted, float opacity)
    {
        IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            bounds.X, bounds.Y, bounds.Width, bounds.Height,
            (highlighted ? Color.Wheat : Color.White) * opacity);
        batch.Draw(Game1.mouseCursors2, bounds.Center.ToVector2(), SettingsSource, Color.White * opacity,
            0f, new Vector2(SettingsSource.Width / 2f, SettingsSource.Height / 2f), 1.65f,
            SpriteEffects.None, 0.95f);
    }

    private void DrawHoverText(SpriteBatch batch, HudScreenState screen, Point mouse, ModConfig config)
    {
        string? text = null;
        for (int index = 0; index < screen.ActionBounds.Count; index++)
        {
            (QuickControlAction action, Rectangle bounds) = screen.ActionBounds[index];
            if (bounds.Contains(mouse))
            {
                text = $"{translate($"config.value.{action.ToString().ToLowerInvariant()}")}\n"
                    + string.Format(translate("hud.quick_controls.shortcut"),
                        ConfigKeybind.FormatBindings(
                            config.GetQuickControlKeybind(index),
                            config.GetQuickControlOptionalKeybind(index)));
                break;
            }
        }

        if (screen.SettingsBounds.Contains(mouse))
            text = translate("hud.quick_controls.settings");
        else if (screen.ExpandBounds.Contains(mouse))
            text = translate(screen.IsExpanded ? "hud.quick_controls.collapse" : "hud.quick_controls.expand");
        else if (screen.ClearBounds.Contains(mouse))
            text = translate("hud.quick_controls.clear");

        if (!string.IsNullOrEmpty(text))
            IClickableMenu.drawHoverText(batch, text, Game1.smallFont);
    }

    private void DrawCompactStatus(
        SpriteBatch batch,
        AutomationSession session,
        ModConfig config,
        Toolbar? toolbar,
        float opacity,
        bool toolbarAtTop,
        bool isFishingMinigame)
    {
        Rectangle bounds = AutomationHudLayout.Place(new AutomationHudLayoutConditions(
            Game1.uiViewport.Width,
            Game1.uiViewport.Height,
            config.ModStatusPosition,
            toolbar?.width ?? 0,
            toolbar?.transparency ?? 0f,
            IsFishingMinigame: isFishingMinigame,
            IsFestival: Game1.isFestival(),
            IsToolbarAtTop: toolbarAtTop));
        AutomationHudVisual visual = AutomationHudVisualPolicy.GetVisual(
            session.IsEnabled, session.State, session.LastReason);
        DrawItem(batch, AutomationHudLayout.PlaceIcon(bounds),
            Game1.player.CurrentTool is StardewValley.Tools.FishingRod rod
                ? rod.QualifiedItemId
                : FallbackRodId,
            Color.White * opacity);
        if (visual.Badge != AutomationHudBadge.None)
            DrawBadge(batch, AutomationHudLayout.PlaceBadge(bounds), visual, opacity);
        if (TreasureTargetingHudVisualPolicy.ShouldDraw(config.TreasureTargeting))
            batch.Draw(Game1.mouseCursors, AutomationHudLayout.PlaceTreasureIcon(bounds),
                TreasureHunterSource, Color.White * opacity);
    }

    private Rectangle PlaceBar(Toolbar? toolbar, bool toolbarAtTop, int actionCount)
    {
        int viewportWidth = Math.Max(1, Game1.uiViewport.Width);
        Rectangle? toolbarBounds = GetDrawnToolbarBounds(toolbar);
        int maximumWidth = Math.Min(
            Math.Max(1, viewportWidth - 16),
            toolbarBounds?.Width ?? Math.Max(1, viewportWidth - 16));
        int requestedWidth = actionCount * 52
            + ButtonGap * (actionCount + 1)
            + MaximumLogWidth
            + SettingsButtonWidth;
        int width = Math.Min(requestedWidth, maximumWidth);
        int x = toolbarBounds is null
            ? (viewportWidth - width) / 2
            : toolbarBounds.Value.Center.X - width / 2;
        x = Math.Clamp(x, 0, Math.Max(0, viewportWidth - width));
        int y = toolbarBounds is null
            ? Game1.uiViewport.Height - BarHeight - 8
            : toolbarAtTop
                ? toolbarBounds.Value.Bottom + 4
                : toolbarBounds.Value.Top - BarHeight - 4;
        y = Math.Clamp(y, 0, Math.Max(0, Game1.uiViewport.Height - BarHeight));
        return new Rectangle(x, y, width, BarHeight);
    }

    private static Rectangle? GetDrawnToolbarBounds(Toolbar? toolbar)
    {
        if (toolbar?.buttons is not { Count: > 0 } buttons)
            return null;

        int left = buttons.Min(button => button.bounds.Left) - 16;
        int right = buttons.Max(button => button.bounds.Right) + 16;
        int top = buttons.Min(button => button.bounds.Top) - 16;
        return new Rectangle(left, top, Math.Max(1, right - left), 96);
    }

    private static bool IsActionActive(QuickControlAction action, AutomationSession session, ModConfig config)
    {
        return action switch
        {
            QuickControlAction.ToggleAutomation => session.IsEnabled,
            QuickControlAction.ToggleTreasureTargeting => config.TreasureTargeting,
            QuickControlAction.ToggleAutoLootTreasure => config.AutoLootTreasure,
            QuickControlAction.ToggleAutoEatFood => config.AutoEatFood,
            QuickControlAction.ToggleJunkDisposal => config.JunkDisposalMode != JunkDisposalMode.Off,
            QuickControlAction.ToggleFishPreview => config.DisplayFishPreview,
            QuickControlAction.ToggleSkipMinigame => config.SkipFishingMiniGame != SkipMinigameBehavior.Off,
            _ => false
        };
    }

    private static void DrawItem(SpriteBatch batch, Rectangle bounds, string itemId, Color color)
    {
        ParsedItemData data = ItemRegistry.GetDataOrErrorItem(itemId);
        Rectangle source = data.GetSourceRect();
        float scale = Math.Min(bounds.Width / (float)source.Width, bounds.Height / (float)source.Height);
        Vector2 position = new(
            bounds.Center.X - source.Width * scale / 2f,
            bounds.Center.Y - source.Height * scale / 2f);
        batch.Draw(data.GetTexture(), position, source, color, 0f, Vector2.Zero, scale,
            SpriteEffects.None, 0.95f);
    }

    private static void DrawBadge(SpriteBatch batch, Rectangle bounds, AutomationHudVisual visual, float opacity)
    {
        int baseEmoteIndex = visual.Badge switch
        {
            AutomationHudBadge.Disabled => Character.xEmote,
            AutomationHudBadge.Paused => Character.pauseEmote,
            AutomationHudBadge.LateNight => Character.sleepEmote,
            AutomationHudBadge.LowEnergy => Character.sadEmote,
            AutomationHudBadge.Warning => Character.exclamationEmote,
            AutomationHudBadge.Recovered => Character.happyEmote,
            AutomationHudBadge.Working => Character.musicNoteEmote,
            _ => -1
        };
        if (baseEmoteIndex < 0)
            return;

        const int sourceSize = 16;
        int emoteIndex = AutomationHudAnimation.GetEmoteFrame(
            baseEmoteIndex, Game1.currentGameTime.TotalGameTime.TotalMilliseconds);
        Rectangle source = new(
            emoteIndex * sourceSize % Game1.emoteSpriteSheet.Width,
            emoteIndex * sourceSize / Game1.emoteSpriteSheet.Width * sourceSize,
            sourceSize,
            sourceSize);
        batch.Draw(Game1.emoteSpriteSheet, bounds, source, Color.White * opacity);
    }

    private static void DrawSeverityIcon(
        SpriteBatch batch,
        Rectangle bounds,
        ActivityLogSeverity severity,
        float opacity)
    {
        int emoteIndex = severity switch
        {
            ActivityLogSeverity.Success => Character.happyEmote,
            ActivityLogSeverity.Warning => Character.exclamationEmote,
            ActivityLogSeverity.Error => Character.xEmote,
            _ => Character.musicNoteEmote
        };
        const int sourceSize = 16;
        Rectangle source = new(
            emoteIndex * sourceSize % Game1.emoteSpriteSheet.Width,
            emoteIndex * sourceSize / Game1.emoteSpriteSheet.Width * sourceSize,
            sourceSize,
            sourceSize);
        batch.Draw(Game1.emoteSpriteSheet, bounds, source, Color.White * opacity);
    }

    private static string FormatEntry(ActivityLogEntry entry)
        => entry.Count > 1 ? $"{entry.Message} ×{entry.Count}" : entry.Message;

    private static Color GetSeverityColor(ActivityLogSeverity severity)
    {
        return severity switch
        {
            ActivityLogSeverity.Success => new Color(55, 112, 53),
            ActivityLogSeverity.Warning => new Color(145, 82, 26),
            ActivityLogSeverity.Error => new Color(151, 45, 39),
            _ => Game1.textColor
        };
    }

    private static bool IsToolbarAtTop()
    {
        if (Game1.options.pinToolbarToggle)
            return false;
        Vector2 position = Game1.GlobalToLocal(Game1.viewport, Game1.player.StandingPixel.ToVector2());
        return position.Y > Game1.uiViewport.Height / 2f + 64f;
    }

    private sealed class HudScreenState
    {
        public bool IsExpanded { get; set; }
        public int ScrollOffset { get; set; }
        public int VisibleRowCount { get; set; } = 1;
        public int LastEntryCount { get; set; }
        public Rectangle BarBounds { get; set; } = Rectangle.Empty;
        public Rectangle SettingsBounds { get; set; } = Rectangle.Empty;
        public Rectangle ExpandBounds { get; set; } = Rectangle.Empty;
        public Rectangle ClearBounds { get; set; } = Rectangle.Empty;
        public Rectangle ExpandedBounds { get; set; } = Rectangle.Empty;
        public List<(QuickControlAction Action, Rectangle Bounds)> ActionBounds { get; } = [];

        public void ClearHitBounds()
        {
            this.BarBounds = Rectangle.Empty;
            this.SettingsBounds = Rectangle.Empty;
            this.ExpandBounds = Rectangle.Empty;
            this.ClearBounds = Rectangle.Empty;
            this.ExpandedBounds = Rectangle.Empty;
            this.ActionBounds.Clear();
        }

        public void Reset()
        {
            this.IsExpanded = false;
            this.ScrollOffset = 0;
            this.VisibleRowCount = 1;
            this.LastEntryCount = 0;
            this.ClearHitBounds();
        }
    }
}

internal readonly record struct QuickControlHudCommand(
    QuickControlHudCommandType Type,
    QuickControlAction Action = QuickControlAction.None)
{
    public static QuickControlHudCommand None { get; } = new(QuickControlHudCommandType.None);
}

internal enum QuickControlHudCommandType
{
    None,
    Action,
    ToggleLog,
    ClearLog,
    OpenSettings
}
