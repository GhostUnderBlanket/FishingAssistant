using FishingAssistant.Configuration;
using FishingAssistant.Debugging;
using FishingAssistant.Equipment;
using FishingAssistant.Fishing;
using FishingAssistant.HUD;
using FishingAssistant.Inventory;
using FishingAssistant.Runtime;
using FishingAssistant.UI;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace FishingAssistant;

internal sealed class ModEntry : Mod
{
    private ConfigManager? configManager;
    private GameItemCatalog? itemCatalog;
    private AutomationRuntime? automationRuntime;
    private AutomationHudRenderer? automationHud;
    private FishPreviewRenderer? fishPreview;
    private StarterFishingRodService? starterFishingRod;
    private DebugWarpService? debugWarp;
    private DebugFishingBubbleService? debugFishingBubble;
    private BaitAttachmentService? baitAttachment;
    private TackleAttachmentService? tackleAttachment;
    private InfiniteAttachmentService? infiniteAttachment;
    private RodEnchantmentService? rodEnchantments;
    private AutoTrashService? autoTrash;

    public override void Entry(IModHelper helper)
    {
        this.configManager = new ConfigManager(
            helper,
            this.Monitor,
            () => Context.IsWorldReady
                ? $"player-{Game1.player.UniqueMultiplayerID}"
                : null);
        this.automationRuntime = new AutomationRuntime(
            this.Monitor,
            () => this.configManager.Active,
            key => helper.Translation.Get(key));
        this.automationHud = new AutomationHudRenderer();
        this.fishPreview = new FishPreviewRenderer(this.Monitor);
        this.starterFishingRod = new StarterFishingRodService(this.Monitor);
        this.debugWarp = new DebugWarpService(this.Monitor, key => helper.Translation.Get(key));
        this.debugFishingBubble = new DebugFishingBubbleService(
            this.Monitor, key => helper.Translation.Get(key));
        this.baitAttachment = new BaitAttachmentService(this.Monitor, key => helper.Translation.Get(key));
        this.tackleAttachment = new TackleAttachmentService(this.Monitor, key => helper.Translation.Get(key));
        this.infiniteAttachment = new InfiniteAttachmentService(this.Monitor);
        this.rodEnchantments = new RodEnchantmentService(this.Monitor, key => helper.Translation.Get(key));
        this.autoTrash = new AutoTrashService(this.Monitor, key => helper.Translation.Get(key));
        ConfigValidationReport report = this.configManager.Load();
        Harmony harmony = new(this.ModManifest.UniqueID);
        CatchResultPatch.Apply(
            harmony,
            () => this.configManager.Active,
            this.Monitor);
        SonarPreviewPatch.Apply(
            harmony,
            () => this.configManager.Active,
            this.Monitor);

        this.Monitor.Log(
            $"Fishing Assistant 3 loaded with {report.Corrections.Count} configuration migration(s) or correction(s) " +
            $"and {report.Warnings.Count} warning(s).",
            LogLevel.Info
        );

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.Saving += this.OnSaving;
        helper.Events.GameLoop.Saved += this.OnSaved;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.Player.Warped += this.OnWarped;
        helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
        helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
        helper.Events.Multiplayer.PeerDisconnected += this.OnPeerDisconnected;
        helper.Events.Display.RenderedHud += this.OnRenderedHud;
        helper.Events.Display.RenderingActiveMenu += this.OnRenderingActiveMenu;
        helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
        helper.ConsoleCommands.Add("fa_config", "Open the Fishing Assistant configuration menu.",
            this.OnConfigCommand);
        helper.ConsoleCommands.Add("fa_bubble", "Create a reachable fishing bubble for testing.",
            this.OnBubbleCommand);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.itemCatalog = new GameItemCatalog();
        ConfigValidationReport report = this.configManager!.ValidateItems(this.itemCatalog);
        if (report.Corrections.Count > 0 || report.Warnings.Count > 0)
        {
            this.Monitor.Log(
                $"Completed game-data configuration validation with {report.Corrections.Count} correction(s) " +
                $"and {report.Warnings.Count} warning(s).",
                LogLevel.Info
            );
        }
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (Game1.activeClickableMenu is ConfigurationMenu { IsListeningForKeybind: true } menu)
        {
            IReadOnlyList<SButton> captured = menu.ReceiveKeybindInput(
                e.Pressed.ToArray(),
                e.Held.ToArray());
            foreach (SButton button in captured)
                this.Helper.Input.Suppress(button);
            return;
        }

        if (Game1.activeClickableMenu is ConfigurationMenu)
            return;

        if (Context.IsWorldReady && this.configManager!.Active.EnableAutomationButton.JustPressed())
        {
            this.Helper.Input.SuppressActiveKeybinds(this.configManager.Active.EnableAutomationButton);
            this.automationRuntime!.ToggleCurrent();
            return;
        }

        KeybindList openConfigKeybind = this.configManager!.Active.OpenConfigMenuButton;
        bool configuredKeybindPressed = openConfigKeybind.JustPressed();
        if (!ConfigurationMenuInput.IsOpenRequested(configuredKeybindPressed, e.Pressed))
            return;

        if (!this.TryOpenConfigMenu())
            return;

        if (configuredKeybindPressed)
            this.Helper.Input.SuppressActiveKeybinds(openConfigKeybind);
        if (e.Pressed.Contains(ConfigurationMenuInput.ControllerFallbackButton))
            this.Helper.Input.Suppress(ConfigurationMenuInput.ControllerFallbackButton);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        this.rodEnchantments!.UpdateCurrent(this.configManager!.Active);
        this.infiniteAttachment!.UpdateCurrent(this.configManager.Active);
        this.baitAttachment!.UpdateCurrent(this.configManager!.Active);
        this.tackleAttachment!.UpdateCurrent(this.configManager.Active);
        this.automationRuntime!.UpdateCurrent();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.rodEnchantments!.RemoveAllAndReset();
        this.infiniteAttachment!.RestoreAll();
        this.infiniteAttachment!.ResetAll();
        this.automationRuntime!.ResetCurrent(AutomationTransitionReason.SaveLoaded);
        this.EnsureConfiguredStarterRod();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.infiniteAttachment!.RestoreCurrent();
        this.automationRuntime!.ResetCurrent(AutomationTransitionReason.DayStarted);
        this.EnsureConfiguredStarterRod();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.rodEnchantments!.RemoveAllAndReset();
        this.infiniteAttachment!.RestoreAll();
        this.infiniteAttachment.ResetAll();
        this.automationRuntime!.ResetAll(AutomationTransitionReason.ReturnedToTitle);
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer)
        {
            this.infiniteAttachment!.RestoreCurrent();
            this.automationRuntime!.ResetCurrent(AutomationTransitionReason.Warped);
        }
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        this.autoTrash!.OnInventoryChanged(
            e,
            this.configManager!.Active,
            this.automationRuntime!.Current.IsEnabled);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        this.automationRuntime!.ResetCurrent(AutomationTransitionReason.Saving);
        this.infiniteAttachment!.RestoreAll();
        this.rodEnchantments!.SuspendAllForSave();
    }

    private void OnSaved(object? sender, SavedEventArgs e)
    {
        this.rodEnchantments!.ResumeAllAfterSave(this.configManager!.Active);
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        this.automationRuntime!.OnTimeChanged(e.NewTime);
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!e.Peer.IsSplitScreen)
            this.rodEnchantments!.RemoveAllForRemoteConnection();
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        this.infiniteAttachment!.RestoreAll();
        this.rodEnchantments!.RemoveAllForRemoteConnection();
        this.automationRuntime!.ResetActiveScreens(AutomationTransitionReason.PeerDisconnected);
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.automationHud!.Draw(e.SpriteBatch, this.automationRuntime!.Current, this.configManager!.Active);
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.fishPreview!.Draw(e.SpriteBatch, this.configManager!.Active);
    }

    private void OnRenderingActiveMenu(object? sender, RenderingActiveMenuEventArgs e)
    {
        SonarPreviewPatch.BeginActiveMenuDraw();
    }

    private void OnConfigCommand(string command, string[] arguments)
    {
        this.TryOpenConfigMenu();
    }

    private void OnBubbleCommand(string command, string[] arguments)
    {
        this.debugFishingBubble!.Create(this.configManager!.Active.DefaultCastPower);
    }

    private bool TryOpenConfigMenu()
    {
        if (Game1.activeClickableMenu is ConfigurationMenu menu)
        {
            menu.exitThisMenu();
            return true;
        }

        if (!Context.IsWorldReady || !Context.IsPlayerFree || Game1.currentMinigame is not null)
        {
            this.Monitor.Log("The configuration menu can't open until a player is free in the world.",
                LogLevel.Info);
            return false;
        }

        Game1.activeClickableMenu = new ConfigurationMenu(
            this.configManager!.CreateEditSession(),
            this.ApplyConfig,
            ConfigManager.CreateDefaultDraft,
            this.itemCatalog!,
            this.Helper.Translation,
            this.debugWarp!.WarpToBeachFishingSpot,
            castPower => this.debugFishingBubble!.Create(castPower)
        );
        return true;
    }

    private ConfigValidationReport ApplyConfig(ConfigEditSession session)
    {
        try
        {
            ConfigValidationReport report = this.configManager!.Apply(session);
            this.automationRuntime!.ResetSessionCastPowerCurrent();
            this.EnsureConfiguredStarterRod();
            return report;
        }
        catch (InvalidOperationException exception)
        {
            this.Monitor.Log($"The configuration draft couldn't be applied: {exception.Message}", LogLevel.Warn);
            throw;
        }
        catch (Exception exception)
        {
            this.Monitor.Log($"The configuration draft couldn't be saved.\n{exception}", LogLevel.Error);
            throw;
        }
    }

    private void EnsureConfiguredStarterRod()
    {
        string itemId = this.configManager!.Active.StartWithFishingRod;
        if (!string.Equals(itemId, "None", StringComparison.OrdinalIgnoreCase))
            this.starterFishingRod!.EnsureRod(itemId);
    }
}
