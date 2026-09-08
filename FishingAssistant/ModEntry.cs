using FishingAssistant.Configuration;
#if FISHING_ASSISTANT_TEST_BUILD
using FishingAssistant.Debugging;
#endif
using FishingAssistant.Equipment;
using FishingAssistant.Fishing;
using FishingAssistant.HUD;
using FishingAssistant.Integrations.GenericModConfigMenu;
using FishingAssistant.Inventory;
using FishingAssistant.Runtime;
using FishingAssistant.UI;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace FishingAssistant;

internal sealed class ModEntry : Mod
{
    private ConfigManager? configManager;
    private GameItemCatalog? itemCatalog;
    private AutomationRuntime? automationRuntime;
    private BobberBarAudioService? bobberBarAudio;
    private AutomationHudRenderer? automationHud;
    private FishingBubbleMarkerRenderer? fishingBubbleMarker;
    private FishPreviewRenderer? fishPreview;
    private StarterFishingRodService? starterFishingRod;
    private BaitAttachmentService? baitAttachment;
    private TackleAttachmentService? tackleAttachment;
    private InfiniteAttachmentService? infiniteAttachment;
    private RodEnchantmentService? rodEnchantments;
    private AutoTrashService? autoTrash;
    private GenericModConfigMenuBridge? genericModConfigMenu;
#if FISHING_ASSISTANT_TEST_BUILD
    private DebugEnergyService? debugEnergy;
    private DebugWarpService? debugWarp;
    private DebugFishingBubbleService? debugFishingBubble;
    private DebugFestivalService? debugFestival;
#endif
    private readonly PerScreen<bool> pendingConfigMenuOpen = new(() => false);

    public override void Entry(IModHelper helper)
    {
        this.configManager = new ConfigManager(
            helper,
            this.Monitor,
            () => Context.IsWorldReady
                ? $"player-{Game1.player.UniqueMultiplayerID}"
                : null);
        PerfectCatchProgressService perfectCatchProgress = new();
        this.bobberBarAudio = new BobberBarAudioService(this.Monitor);
        this.automationRuntime = new AutomationRuntime(
            this.Monitor,
            () => this.configManager.Active,
            key => helper.Translation.Get(key),
            perfectCatchProgress,
            this.bobberBarAudio);
        this.automationHud = new AutomationHudRenderer();
        this.fishingBubbleMarker = new FishingBubbleMarkerRenderer(
            () => this.automationRuntime.GetBubbleMarkerPlanCurrent());
        this.fishPreview = new FishPreviewRenderer(this.Monitor);
        this.starterFishingRod = new StarterFishingRodService(this.Monitor);
        this.baitAttachment = new BaitAttachmentService(this.Monitor, key => helper.Translation.Get(key));
        this.tackleAttachment = new TackleAttachmentService(this.Monitor, key => helper.Translation.Get(key));
        this.infiniteAttachment = new InfiniteAttachmentService(this.Monitor);
        this.rodEnchantments = new RodEnchantmentService(this.Monitor, key => helper.Translation.Get(key));
        this.autoTrash = new AutoTrashService(this.Monitor, key => helper.Translation.Get(key));
#if FISHING_ASSISTANT_TEST_BUILD
        this.debugEnergy = new DebugEnergyService(this.Monitor, key => helper.Translation.Get(key));
        this.debugWarp = new DebugWarpService(this.Monitor, key => helper.Translation.Get(key));
        this.debugFishingBubble = new DebugFishingBubbleService(
            this.Monitor, key => helper.Translation.Get(key));
        this.debugFestival = new DebugFestivalService(this.Monitor, key => helper.Translation.Get(key));
#endif
        this.genericModConfigMenu = new GenericModConfigMenuBridge(
            helper,
            this.ModManifest,
            this.Monitor,
            this.TryOpenConfigMenu,
            () => helper.Translation.Get("integration.gmcm.load_save"));
        ConfigValidationReport report = this.configManager.Load();
        Harmony harmony = new(this.ModManifest.UniqueID);
        CatchResultPatch.Apply(
            harmony,
            () => this.configManager.Active,
            perfectCatchProgress,
            () => this.automationRuntime.IsCurrentMinigameSkipResult(),
            this.Monitor);
        FishingTreasureProtectionPatch.Apply(harmony, this.Monitor);
        SonarPreviewPatch.Apply(
            harmony,
            () => this.configManager.Active,
            this.Monitor);
        MinigameAssistancePatch.Apply(
            harmony,
            () => this.configManager.Active,
            this.Monitor);
        FishingGameHudPatch.Apply(
            harmony,
            this.automationHud,
            () => this.automationRuntime!.Current,
            () => this.configManager!.Active,
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
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        helper.Events.Display.RenderingActiveMenu += this.OnRenderingActiveMenu;
        helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
        helper.Events.Display.MenuChanged += this.OnMenuChanged;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
        helper.ConsoleCommands.Add("fa_config", "Open the Fishing Assistant configuration menu.",
            this.OnConfigCommand);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.itemCatalog = new GameItemCatalog();
        this.genericModConfigMenu!.Register();
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

        KeybindList automationKeybind = this.configManager!.Active.EnableAutomationButton;
        KeybindList automationOptionalKeybind = this.configManager.Active.EnableAutomationOptionalButton;
        bool automationKeybindPressed = automationKeybind.JustPressed();
        bool automationOptionalKeybindPressed = automationOptionalKeybind.JustPressed();
        if (Context.IsWorldReady && (automationKeybindPressed || automationOptionalKeybindPressed))
        {
            if (automationKeybindPressed)
                this.Helper.Input.SuppressActiveKeybinds(automationKeybind);
            if (automationOptionalKeybindPressed)
                this.Helper.Input.SuppressActiveKeybinds(automationOptionalKeybind);
            this.automationRuntime!.ToggleCurrent();
            if (this.automationRuntime.Current.IsEnabled)
                this.autoTrash!.TryDiscardBatchIfFull(
                    Game1.player,
                    this.configManager.Active,
                    automationEnabled: true,
                    hasFishingRod: Game1.player.CurrentTool is FishingRod);
            return;
        }

        KeybindList treasureKeybind = this.configManager!.Active.ToggleTreasureTargetingButton;
        KeybindList treasureOptionalKeybind =
            this.configManager.Active.ToggleTreasureTargetingOptionalButton;
        bool treasureKeybindPressed = treasureKeybind.JustPressed();
        bool treasureOptionalKeybindPressed = treasureOptionalKeybind.JustPressed();
        if (Context.IsWorldReady && (treasureKeybindPressed || treasureOptionalKeybindPressed))
        {
            if (treasureKeybindPressed)
                this.Helper.Input.SuppressActiveKeybinds(treasureKeybind);
            if (treasureOptionalKeybindPressed)
                this.Helper.Input.SuppressActiveKeybinds(treasureOptionalKeybind);
            try
            {
                bool enabled = this.configManager.ToggleTreasureTargeting();
                Game1.playSound(enabled ? "coin" : "bigDeSelect");
            }
            catch (InvalidOperationException exception)
            {
                this.Monitor.Log(
                    $"Treasure targeting couldn't be toggled: {exception.Message}",
                    LogLevel.Warn);
            }
            return;
        }

        KeybindList openConfigKeybind = this.configManager!.Active.OpenConfigMenuButton;
        KeybindList openConfigOptionalKeybind =
            this.configManager.Active.OpenConfigMenuOptionalButton;
        bool configuredKeybindPressed = openConfigKeybind.JustPressed();
        bool configuredOptionalKeybindPressed = openConfigOptionalKeybind.JustPressed();
        if (!configuredKeybindPressed && !configuredOptionalKeybindPressed)
            return;

        if (!this.TryOpenConfigMenu())
            return;

        if (configuredKeybindPressed)
            this.Helper.Input.SuppressActiveKeybinds(openConfigKeybind);
        if (configuredOptionalKeybindPressed)
            this.Helper.Input.SuppressActiveKeybinds(openConfigOptionalKeybind);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        this.genericModConfigMenu!.UpdateCurrent();
        this.TryCompletePendingConfigMenuOpen();
        this.rodEnchantments!.UpdateCurrent(this.configManager!.Active);
        this.infiniteAttachment!.UpdateCurrent(this.configManager.Active);
        this.baitAttachment!.UpdateCurrent(this.configManager!.Active);
        this.tackleAttachment!.UpdateCurrent(this.configManager.Active);
        this.automationRuntime!.UpdateCurrent();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.bobberBarAudio!.ResetCurrent(AutomationTransitionReason.SaveLoaded);
        this.rodEnchantments!.RemoveAllAndReset();
        this.infiniteAttachment!.RestoreAll();
        this.infiniteAttachment!.ResetAll();
        this.automationRuntime!.ResetCurrent(AutomationTransitionReason.SaveLoaded);
        this.EnsureConfiguredStarterRod();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.bobberBarAudio!.ResetCurrent(AutomationTransitionReason.DayStarted);
        this.infiniteAttachment!.RestoreCurrent();
        this.automationRuntime!.ResetCurrent(AutomationTransitionReason.DayStarted);
        this.EnsureConfiguredStarterRod();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.bobberBarAudio!.ResetAll(AutomationTransitionReason.ReturnedToTitle);
        this.pendingConfigMenuOpen.ResetAllScreens();
        this.genericModConfigMenu!.Reset();
        this.rodEnchantments!.RemoveAllAndReset();
        this.infiniteAttachment!.RestoreAll();
        this.infiniteAttachment.ResetAll();
        this.automationRuntime!.ResetAll(AutomationTransitionReason.ReturnedToTitle);
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer)
        {
            this.bobberBarAudio!.ResetCurrent(AutomationTransitionReason.Warped);
            this.pendingConfigMenuOpen.Value = false;
            this.infiniteAttachment!.RestoreCurrent();
            this.automationRuntime!.ResetCurrent(AutomationTransitionReason.Warped);
        }
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        this.autoTrash!.OnInventoryChanged(
            e,
            this.configManager!.Active,
            this.automationRuntime!.Current.IsEnabled,
            e.Player.CurrentTool is FishingRod);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        this.bobberBarAudio!.ResetCurrent(AutomationTransitionReason.Saving);
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

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        this.fishingBubbleMarker!.Draw(e.SpriteBatch, this.configManager!.Active);
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.currentMinigame is StardewValley.Minigames.FishingGame)
            return;

        this.automationHud!.Draw(e.SpriteBatch, this.automationRuntime!.Current, this.configManager!.Active);
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.fishPreview!.Draw(e.SpriteBatch, this.configManager!.Active);
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        this.bobberBarAudio!.OnMenuChanged(e.OldMenu, e.NewMenu);
        if (!Context.IsWorldReady || e.NewMenu is not BobberBar bobberBar)
            return;

        this.automationRuntime!.TrySkipBeforeFirstDraw(bobberBar);
    }

    private void OnRenderingActiveMenu(object? sender, RenderingActiveMenuEventArgs e)
    {
        SonarPreviewPatch.BeginActiveMenuDraw();
    }

    private void OnConfigCommand(string command, string[] arguments)
    {
        this.TryOpenConfigMenu();
    }

    private bool TryOpenConfigMenu()
    {
        if (Game1.activeClickableMenu is ConfigurationMenu menu)
        {
            menu.RequestClose();
            return true;
        }

        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod?.IsCastInProgress == true)
        {
            if (!this.pendingConfigMenuOpen.Value)
            {
                this.pendingConfigMenuOpen.Value = true;
                Game1.addHUDMessage(new HUDMessage(
                    this.Helper.Translation.Get("hud.config_wait_for_cast"),
                    HUDMessage.newQuest_type));
            }
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
            this.Helper.Translation
#if FISHING_ASSISTANT_TEST_BUILD
            , new DebugMenuActions(
                this.debugEnergy!.SetLowEnergy,
                this.debugEnergy!.RestoreEnergy,
                this.debugWarp!.WarpToBeachFishingSpot,
                castPower => this.debugFishingBubble!.Create(castPower),
                this.debugFestival!.PrepareIceFishingFestival,
                this.debugFestival!.PrepareStardewValleyFair)
#endif
        );
        return true;
    }

    private void TryCompletePendingConfigMenuOpen()
    {
        if (!this.pendingConfigMenuOpen.Value)
            return;

        if (FishingRodAdapter.ForCurrentPlayer()?.IsCastInProgress == true)
            return;

        this.pendingConfigMenuOpen.Value = false;
        this.TryOpenConfigMenu();
    }

    private ConfigValidationReport ApplyConfig(ConfigEditSession session)
    {
        try
        {
            ConfigValidationReport report = this.configManager!.Apply(session);
            this.automationRuntime!.ResetSessionCastPowerCurrent();
            this.automationRuntime.InvalidateTreasureChestIgnoreCacheCurrent();
            this.EnsureConfiguredStarterRod();
            this.autoTrash!.TryDiscardBatchIfFull(
                Game1.player,
                this.configManager.Active,
                this.automationRuntime.Current.IsEnabled,
                Game1.player.CurrentTool is FishingRod);
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
