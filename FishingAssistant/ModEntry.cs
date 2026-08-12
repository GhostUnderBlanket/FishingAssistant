using FishingAssistant.Configuration;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace FishingAssistant;

internal sealed class ModEntry : Mod
{
    private ConfigManager? configManager;

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
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ConfigValidationReport report = this.configManager!.ValidateItems(new GameItemCatalog());
        if (report.Corrections.Count > 0 || report.Warnings.Count > 0)
        {
            this.Monitor.Log(
                $"Completed game-data configuration validation with {report.Corrections.Count} correction(s) " +
                $"and {report.Warnings.Count} warning(s).",
                LogLevel.Info
            );
        }
    }
}
