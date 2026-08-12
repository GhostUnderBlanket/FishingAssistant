using FishingAssistant.Configuration;
using StardewModdingAPI;

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
    }
}
