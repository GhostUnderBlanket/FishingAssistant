using StardewModdingAPI;

namespace FishingAssistant.Configuration;

internal sealed class ConfigManager(IModHelper helper, IMonitor monitor)
{
    private const string ConfigFileName = "config.json";
    private bool loadedFutureSchema;

    public ModConfig Active { get; private set; } = new();

    public ConfigValidationReport Load()
    {
        ConfigFileMetadata metadata = this.InspectExistingFile();
        ModConfig loaded = helper.ReadConfig<ModConfig>();
        ConfigValidationReport report = ConfigValidator.Normalize(loaded);

        if (metadata.IsLegacy)
        {
            report.Add(nameof(ModConfig.ConfigVersion), "missing", ModConfig.CurrentVersion,
                "The Fishing Assistant 2 configuration was migrated to version 3.");
        }

        foreach (string property in metadata.UnknownProperties)
        {
            report.Warn(property, "unknown",
                "This property isn't recognized by Fishing Assistant 3 and wasn't migrated.");
        }

        this.loadedFutureSchema = loaded.ConfigVersion > ModConfig.CurrentVersion;

        this.Active = loaded;
        if (report.WasChanged && !this.loadedFutureSchema)
        {
            helper.WriteConfig(this.Active);
            this.LogCorrections(report);
        }

        this.LogWarnings(report);

        return report;
    }

    public ConfigValidationReport Apply(ModConfig draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (this.loadedFutureSchema)
        {
            throw new InvalidOperationException(
                "A configuration from a newer Fishing Assistant version is loaded and can't be overwritten."
            );
        }

        ModConfig validated = draft.CreateDraft();
        ConfigValidationReport report = ConfigValidator.Normalize(validated);
        helper.WriteConfig(validated);
        this.Active = validated;
        this.LogCorrections(report);
        return report;
    }

    public ModConfig CreateDraft()
    {
        return this.Active.CreateDraft();
    }

    public static ModConfig CreateDefaultDraft()
    {
        return new ModConfig();
    }

    private ConfigFileMetadata InspectExistingFile()
    {
        string path = Path.Combine(helper.DirectoryPath, ConfigFileName);
        if (!File.Exists(path))
            return ConfigFileMetadata.Empty;

        try
        {
            string json = File.ReadAllText(path);
            return new ConfigFileMetadata(
                ConfigSchemaInspector.IsLegacyJson(json),
                ConfigSchemaInspector.FindUnknownProperties(json)
            );
        }
        catch (System.Text.Json.JsonException)
        {
            return ConfigFileMetadata.Empty;
        }
    }

    private void LogCorrections(ConfigValidationReport report)
    {
        foreach (ConfigCorrection correction in report.Corrections)
        {
            monitor.Log(
                $"Normalized config '{correction.Property}' from '{correction.OriginalValue}' " +
                $"to '{correction.CorrectedValue}': {correction.Reason}",
                LogLevel.Warn
            );
        }
    }

    private void LogWarnings(ConfigValidationReport report)
    {
        foreach (ConfigWarning warning in report.Warnings)
        {
            monitor.Log(
                $"Config warning for '{warning.Property}' with value '{warning.Value}': {warning.Reason}",
                LogLevel.Warn
            );
        }
    }

    private sealed record ConfigFileMetadata(bool IsLegacy, IReadOnlyList<string> UnknownProperties)
    {
        public static ConfigFileMetadata Empty { get; } = new(false, []);
    }
}
