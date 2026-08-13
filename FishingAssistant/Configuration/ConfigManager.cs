using StardewModdingAPI;

namespace FishingAssistant.Configuration;

internal sealed class ConfigManager(
    IModHelper helper,
    IMonitor monitor,
    Func<string?>? profileKeyProvider = null,
    IConfigProfileStore? profileStore = null)
{
    private const string ConfigFileName = "config.json";
    private readonly Dictionary<string, ProfileState> profiles = new(StringComparer.Ordinal);
    private readonly Func<string?> profileKeyProvider = profileKeyProvider ?? (() => null);
    private readonly IConfigProfileStore profileStore = profileStore ?? new ConfigProfileStore(helper.Data);
    private IItemCatalog? itemCatalog;
    private bool loadedFutureSchema;
    private int revision;

    private ModConfig template = new();

    public ModConfig Active => this.GetCurrentState()?.Config ?? this.template;

    public ConfigValidationReport Load()
    {
        ConfigFileMetadata metadata = this.InspectExistingFile();
        ModConfig loaded;
        try
        {
            loaded = helper.ReadConfig<ModConfig>();
        }
        catch (Exception exception)
        {
            loaded = new ModConfig();
            this.template = loaded;
            this.revision++;

            ConfigValidationReport failedReport = new();
            failedReport.Warn(ConfigFileName, exception.GetType().Name,
                "The configuration couldn't be read. Safe defaults are active for this session, " +
                "and the original file wasn't overwritten.");
            monitor.Log($"Couldn't read {ConfigFileName}; using safe defaults without overwriting it.\n{exception}",
                LogLevel.Error);
            return failedReport;
        }

        ConfigValidationReport report = ConfigValidator.Normalize(loaded);

        if (metadata.IsLegacy)
        {
            report.Add(nameof(ModConfig.ConfigVersion), "missing", ModConfig.CurrentVersion,
                "The Fishing Assistant 2 configuration was migrated to the current schema.");
        }

        foreach (ConfigPropertySnapshot property in metadata.RetiredProperties)
        {
            string reason = property.Name.Equals("JunkHighestPrice", StringComparison.OrdinalIgnoreCase)
                ? "This Fishing Assistant 2 price threshold was retired; use the visual Junk List editor instead."
                : "This Fishing Assistant 2 treasure-targeting hotkey was retired; use the config-menu setting instead.";
            report.Warn(property.Name, property.DisplayValue, reason);
        }

        foreach (ConfigPropertySnapshot property in metadata.UnknownProperties)
        {
            report.Warn(property.Name, property.DisplayValue,
                "This property isn't recognized by Fishing Assistant 3 and wasn't migrated.");
        }

        this.loadedFutureSchema = loaded.ConfigVersion > ModConfig.CurrentVersion;

        this.template = loaded;
        this.revision++;
        if (report.WasChanged && !this.loadedFutureSchema)
            helper.WriteConfig(this.template);

        this.LogCorrections(report);
        this.LogWarnings(report);

        return report;
    }

    public ConfigValidationReport Apply(ConfigEditSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        string? currentKey = this.profileKeyProvider();
        if (!string.Equals(session.ProfileKey, currentKey, StringComparison.Ordinal))
            throw new InvalidOperationException("The local player changed after this menu opened.");

        ProfileState? profile = this.GetCurrentState();
        int currentRevision = profile?.Revision ?? this.revision;
        session.EnsureCurrent(currentRevision);
        string? readOnlyReason = profile?.ReadOnlyReason;
        if (readOnlyReason is not null || (profile is null && this.loadedFutureSchema))
        {
            throw new InvalidOperationException(
                readOnlyReason
                ?? "A configuration from a newer Fishing Assistant version is loaded and can't be overwritten."
            );
        }

        ModConfig validated = session.Draft.CreateDraft();
        ConfigValidationReport report = ConfigValidator.Normalize(validated, this.itemCatalog);
        if (currentKey is null)
        {
            helper.WriteConfig(validated);
            this.template = validated;
            this.revision++;
        }
        else
        {
            this.profileStore.Write(currentKey, validated);
            this.profiles[currentKey] = new ProfileState(validated, currentRevision + 1, null);
        }
        this.LogCorrections(report);
        this.LogWarnings(report);
        return report;
    }

    public ConfigValidationReport ValidateItems(IItemCatalog itemCatalog)
    {
        ArgumentNullException.ThrowIfNull(itemCatalog);

        this.itemCatalog = itemCatalog;
        ConfigValidationReport report = ConfigValidator.NormalizeItems(this.template, itemCatalog);
        if (report.WasChanged && !this.loadedFutureSchema)
        {
            helper.WriteConfig(this.template);
            this.revision++;
        }

        foreach ((string key, ProfileState profile) in this.profiles.ToArray())
        {
            ConfigValidationReport profileReport = ConfigValidator.NormalizeItems(profile.Config, itemCatalog);
            report.Append(profileReport);
            if (profileReport.WasChanged && profile.ReadOnlyReason is null)
            {
                this.profileStore.Write(key, profile.Config);
                this.profiles[key] = profile with { Revision = profile.Revision + 1 };
            }
        }

        this.LogCorrections(report);
        this.LogWarnings(report);
        return report;
    }

    public ConfigEditSession CreateEditSession()
    {
        string? profileKey = this.profileKeyProvider();
        ProfileState? profile = this.GetCurrentState();
        return new ConfigEditSession(
            (profile?.Config ?? this.template).CreateDraft(),
            profile?.Revision ?? this.revision,
            profileKey);
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
                ConfigSchemaInspector.FindUnknownProperties(json),
                ConfigSchemaInspector.FindRetiredProperties(json)
            );
        }
        catch (System.Text.Json.JsonException)
        {
            return ConfigFileMetadata.Empty;
        }
    }

    private ProfileState? GetCurrentState()
    {
        string? key = this.profileKeyProvider();
        if (key is null)
            return null;

        if (this.profiles.TryGetValue(key, out ProfileState? existing))
            return existing;

        ModConfig config;
        string? readOnlyReason;
        try
        {
            config = this.profileStore.Read(key) ?? this.template.CreateDraft();
            readOnlyReason = config.ConfigVersion > ModConfig.CurrentVersion
                ? "This player profile comes from a newer Fishing Assistant version and can't be overwritten."
                : null;
            ConfigValidationReport report = ConfigValidator.Normalize(config, this.itemCatalog);
            this.LogCorrections(report);
            this.LogWarnings(report);
            if (report.WasChanged && readOnlyReason is null)
                this.profileStore.Write(key, config);
        }
        catch (Exception exception)
        {
            config = this.template.CreateDraft();
            readOnlyReason = "This player profile couldn't be read and won't be overwritten.";
            monitor.Log(
                $"Couldn't read configuration profile '{key}'; using the base configuration without overwriting the profile.\n{exception}",
                LogLevel.Error);
        }

        ProfileState created = new(config, 1, readOnlyReason);
        this.profiles[key] = created;
        return created;
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

    private sealed record ConfigFileMetadata(
        bool IsLegacy,
        IReadOnlyList<ConfigPropertySnapshot> UnknownProperties,
        IReadOnlyList<ConfigPropertySnapshot> RetiredProperties)
    {
        public static ConfigFileMetadata Empty { get; } = new(false, [], []);
    }

    private sealed record ProfileState(ModConfig Config, int Revision, string? ReadOnlyReason);
}
