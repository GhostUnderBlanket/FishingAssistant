using FishingAssistant.Configuration;
using FishingAssistant.Debugging;
using FishingAssistant.Equipment;
using FishingAssistant.HUD;
using FishingAssistant.Runtime;
using FishingAssistant.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace FishingAssistant;

internal sealed class ModEntry : Mod
{
    private ConfigManager? configManager;
    private GameItemCatalog? itemCatalog;
    private AutomationRuntime? automationRuntime;
    private AutomationHudRenderer? automationHud;
    private StarterFishingRodService? starterFishingRod;
    private DebugWarpService? debugWarp;
    private BaitAttachmentService? baitAttachment;
    private TackleAttachmentService? tackleAttachment;
    private InfiniteAttachmentService? infiniteAttachment;
    private RodEnchantmentService? rodEnchantments;

    public override void Entry(IModHelper helper)
    {
        this.configManager = new ConfigManager(helper, this.Monitor);
        this.automationRuntime = new AutomationRuntime(
            this.Monitor,
            () => this.configManager.Active,
            key => helper.Translation.Get(key));
        this.automationHud = new AutomationHudRenderer(key => helper.Translation.Get(key));
        this.starterFishingRod = new StarterFishingRodService(this.Monitor, key => helper.Translation.Get(key));
        this.debugWarp = new DebugWarpService(this.Monitor, key => helper.Translation.Get(key));
        this.baitAttachment = new BaitAttachmentService(this.Monitor, key => helper.Translation.Get(key));
        this.tackleAttachment = new TackleAttachmentService(this.Monitor, key => helper.Translation.Get(key));
        this.infiniteAttachment = new InfiniteAttachmentService(this.Monitor);
        this.rodEnchantments = new RodEnchantmentService(this.Monitor, key => helper.Translation.Get(key));
        ConfigValidationReport report = this.configManager.Load();

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
        helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
        helper.Events.Display.RenderedHud += this.OnRenderedHud;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
        helper.ConsoleCommands.Add("fa_config", "Open the Fishing Assistant configuration menu.",
            this.OnConfigCommand);
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
            if (!e.Pressed.Any())
                return;

            SButton[] buttons = e.Held
                .Concat(e.Pressed)
                .Where(button => button != SButton.None)
                .Distinct()
                .ToArray();
            foreach (SButton button in e.Pressed)
                this.Helper.Input.Suppress(button);

            menu.ReceiveKeybindInput(buttons);
            return;
        }

        if (Game1.activeClickableMenu is ConfigurationMenu)
        {
            if (this.configManager!.Active.OpenConfigMenuButton.JustPressed())
            {
                this.Helper.Input.SuppressActiveKeybinds(this.configManager.Active.OpenConfigMenuButton);
                this.TryOpenConfigMenu();
            }
            return;
        }

        if (Context.IsWorldReady && this.configManager!.Active.EnableAutomationButton.JustPressed())
        {
            this.Helper.Input.SuppressActiveKeybinds(this.configManager.Active.EnableAutomationButton);
            this.automationRuntime!.ToggleCurrent();
            return;
        }

        if (Context.IsWorldReady && this.configManager!.Active.CatchTreasureButton.JustPressed())
        {
            this.Helper.Input.SuppressActiveKeybinds(this.configManager.Active.CatchTreasureButton);
            this.automationRuntime!.ToggleTreasureTargetingCurrent();
            return;
        }

        if (!this.configManager!.Active.OpenConfigMenuButton.JustPressed())
            return;

        this.Helper.Input.SuppressActiveKeybinds(this.configManager.Active.OpenConfigMenuButton);
        this.TryOpenConfigMenu();
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
        this.rodEnchantments!.ResetAll();
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

    private void OnSaving(object? sender, SavingEventArgs e)
    {
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

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.automationHud!.Draw(e.SpriteBatch, this.automationRuntime!.Current, this.configManager!.Active);
    }

    private void OnConfigCommand(string command, string[] arguments)
    {
        this.TryOpenConfigMenu();
    }

    private void TryOpenConfigMenu()
    {
        if (Game1.activeClickableMenu is ConfigurationMenu menu)
        {
            menu.exitThisMenu();
            return;
        }

        if (!Context.IsWorldReady || !Context.IsPlayerFree || Game1.currentMinigame is not null)
        {
            this.Monitor.Log("The configuration menu can't open until a player is free in the world.",
                LogLevel.Info);
            return;
        }

        Game1.activeClickableMenu = new ConfigurationMenu(
            this.configManager!.CreateEditSession(),
            this.ApplyConfig,
            ConfigManager.CreateDefaultDraft,
            this.itemCatalog!,
            this.Helper.Translation,
            this.starterFishingRod!.AddTestRodFromMenu,
            this.debugWarp!.WarpToBeachFishingSpot
        );
    }

    private ConfigValidationReport ApplyConfig(ConfigEditSession session)
    {
        try
        {
            ConfigValidationReport report = this.configManager!.Apply(session);
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
