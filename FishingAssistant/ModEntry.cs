using FishingAssistant.Configuration;
using FishingAssistant.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace FishingAssistant;

internal sealed class ModEntry : Mod
{
    private ConfigManager? configManager;
    private GameItemCatalog? itemCatalog;

    public override void Entry(IModHelper helper)
    {
        this.configManager = new ConfigManager(helper, this.Monitor);
        ConfigValidationReport report = this.configManager.Load();

        this.Monitor.Log(
            $"Fishing Assistant 3 loaded with {report.Corrections.Count} configuration migration(s) or correction(s) " +
            $"and {report.Warnings.Count} warning(s).",
            LogLevel.Info
        );

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
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

        if (!this.configManager!.Active.OpenConfigMenuButton.JustPressed())
            return;

        this.Helper.Input.SuppressActiveKeybinds(this.configManager.Active.OpenConfigMenuButton);
        this.TryOpenConfigMenu();
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
            this.Helper.Translation
        );
    }

    private ConfigValidationReport ApplyConfig(ConfigEditSession session)
    {
        try
        {
            return this.configManager!.Apply(session);
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
}
