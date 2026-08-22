using FishingAssistant.Fishing;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace FishingAssistant.Configuration;

internal static class ConfigValidator
{
    private const string InvalidEnumReason = "The value isn't supported by Fishing Assistant 3.";
    private const string OutOfRangeReason = "The value was outside the supported range.";

    public static ConfigValidationReport Normalize(ModConfig config, IItemCatalog? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        ConfigValidationReport report = new();

        int originalVersion = config.ConfigVersion;
        NormalizeVersion(config, report);
        RetireJunkIgnoreList(config, originalVersion, report);
        MigrateJunkDisposalMode(config, originalVersion, report);
        MigrateCastPowerAdjustmentMode(config, originalVersion, report);
        MigrateOrderedEquipmentPreferences(config, originalVersion, report);
        MigrateFishDifficultySettings(config, originalVersion, report);
        if (originalVersion < 12)
        {
            config.FishPreviewStyle = FishPreviewStyle.Classic;
            report.Add(nameof(config.FishPreviewStyle), null, config.FishPreviewStyle,
                "The existing fish preview appearance was preserved during migration.");
        }
        if (originalVersion < 9)
        {
            config.AutomationProfile = AutomationProfile.Custom;
            report.Add(nameof(config.AutomationProfile), null, config.AutomationProfile,
                "Existing individually configured automation settings were preserved as Custom.");
        }
        else if (originalVersion < 10
                 && config.AutomationProfile is AutomationProfile.Relaxed or AutomationProfile.Training)
        {
            config.AutomaticBubbleSteering = true;
            report.Add(nameof(config.AutomaticBubbleSteering), false, true,
                "Bubble steering was enabled for the selected automation profile.");
        }
        NormalizeKeybind(report, nameof(config.EnableAutomationButton),
            () => config.EnableAutomationButton, value => config.EnableAutomationButton = value, SButton.F5);
        NormalizeKeybind(report, nameof(config.OpenConfigMenuButton),
            () => config.OpenConfigMenuButton, value => config.OpenConfigMenuButton = value, SButton.F6);
        NormalizeKeybind(report, nameof(config.ToggleTreasureTargetingButton),
            () => config.ToggleTreasureTargetingButton,
            value => config.ToggleTreasureTargetingButton = value,
            SButton.None);

        NormalizeEnum(report, nameof(config.ModStatusPosition),
            () => config.ModStatusPosition, value => config.ModStatusPosition = value, HudPosition.Left);
        NormalizeEnum(report, nameof(config.FishPreviewStyle),
            () => config.FishPreviewStyle, value => config.FishPreviewStyle = value, FishPreviewStyle.Classic);
        NormalizeEnum(report, nameof(config.AutomationProfile),
            () => config.AutomationProfile, value => config.AutomationProfile = value, AutomationProfile.Custom);
        NormalizeEnum(report, nameof(config.MinigameAssistance),
            () => config.MinigameAssistance, value => config.MinigameAssistance = value,
            MinigameAssistancePreset.Off);
        NormalizeEnum(report, nameof(config.SteeringEffort),
            () => config.SteeringEffort, value => config.SteeringEffort = value,
            SteeringEffort.Normal);
        NormalizeEnum(report, nameof(config.AutomaticCastPowerAdjustmentMode),
            () => config.AutomaticCastPowerAdjustmentMode,
            value => config.AutomaticCastPowerAdjustmentMode = value, CastPowerAdjustmentMode.Off);
        NormalizeEnum(report, nameof(config.ActionIfInventoryFull),
            () => config.ActionIfInventoryFull, value => config.ActionIfInventoryFull = value, InventoryFullAction.Stop);
        NormalizeEnum(report, nameof(config.ActionIfOnlyIgnoredTreasureRemains),
            () => config.ActionIfOnlyIgnoredTreasureRemains,
            value => config.ActionIfOnlyIgnoredTreasureRemains = value,
            IgnoredTreasureAction.KeepOpen);
        NormalizeEnum(report, nameof(config.JunkDisposalMode),
            () => config.JunkDisposalMode,
            value => config.JunkDisposalMode = value,
            JunkDisposalMode.Off);
        NormalizeEnum(report, nameof(config.AutoPauseFishing),
            () => config.AutoPauseFishing, value => config.AutoPauseFishing = value, PauseFishingBehavior.WarnAndPause);
        NormalizeEnum(report, nameof(config.SkipFishingMiniGame),
            () => config.SkipFishingMiniGame, value => config.SkipFishingMiniGame = value, SkipMinigameBehavior.Off);
        NormalizeEnum(report, nameof(config.PreferFishQuality),
            () => config.PreferFishQuality, value => config.PreferFishQuality = value, FishQualityPreference.Any);
        NormalizeEnum(report, nameof(config.TreasureChance),
            () => config.TreasureChance, value => config.TreasureChance = value, TreasureChanceBehavior.Default);
        NormalizeEnum(report, nameof(config.GoldenTreasureChance),
            () => config.GoldenTreasureChance, value => config.GoldenTreasureChance = value, TreasureChanceBehavior.Default);

        NormalizeRange(report, nameof(config.TimeToPause),
            () => config.TimeToPause, value => config.TimeToPause = value, 6, 25);
        NormalizeRange(report, nameof(config.WarnCount),
            () => config.WarnCount, value => config.WarnCount = value, 1, 5);
        NormalizeRange(report, nameof(config.EnergyPercentToEat),
            () => config.EnergyPercentToEat, value => config.EnergyPercentToEat = value, 5, 95);
        NormalizeRange(report, nameof(config.BaitAmountToSpawn),
            () => config.BaitAmountToSpawn, value => config.BaitAmountToSpawn = value, 1, 999);
        NormalizeRange(report, nameof(config.PreferFishAmount),
            () => config.PreferFishAmount, value => config.PreferFishAmount = value, 1, 3);
        NormalizeRange(report, nameof(config.FishSpeedPercent),
            () => config.FishSpeedPercent, value => config.FishSpeedPercent = value,
            MinigameAssistancePolicy.FishSpeedMinimum, MinigameAssistancePolicy.FishSpeedMaximum);
        NormalizeRange(report, nameof(config.ProgressGainPercent),
            () => config.ProgressGainPercent, value => config.ProgressGainPercent = value,
            MinigameAssistancePolicy.ProgressGainMinimum, MinigameAssistancePolicy.ProgressGainMaximum);
        NormalizeRange(report, nameof(config.ProgressLossPercent),
            () => config.ProgressLossPercent, value => config.ProgressLossPercent = value,
            MinigameAssistancePolicy.ProgressLossMinimum, MinigameAssistancePolicy.ProgressLossMaximum);
        NormalizeRange(report, nameof(config.TreasureSpeedPercent),
            () => config.TreasureSpeedPercent, value => config.TreasureSpeedPercent = value,
            MinigameAssistancePolicy.TreasureSpeedMinimum, MinigameAssistancePolicy.TreasureSpeedMaximum);
        NormalizeRange(report, nameof(config.BarSizePercent),
            () => config.BarSizePercent, value => config.BarSizePercent = value,
            MinigameAssistancePolicy.BarSizeMinimum, MinigameAssistancePolicy.BarSizeMaximum);
        NormalizeRange(report, nameof(config.DefaultCastPower),
            () => config.DefaultCastPower, value => config.DefaultCastPower = value, 0, 100);
        NormalizeFloatRange(report, nameof(config.AutoCastDelaySeconds),
            () => config.AutoCastDelaySeconds, value => config.AutoCastDelaySeconds = value, 0f, 10f);
        NormalizeFloatRange(report, nameof(config.UnlockCastPowerTime),
            () => config.UnlockCastPowerTime, value => config.UnlockCastPowerTime = value, 0f, 3f);

        NormalizeString(report, nameof(config.StartWithFishingRod),
            () => config.StartWithFishingRod, value => config.StartWithFishingRod = value,
            ModConfig.DefaultStarterRod);
        NormalizeItemList(report, nameof(config.JunkList),
            () => config.JunkList, value => config.JunkList = value);
        NormalizeItemList(report, nameof(config.TreasureChestIgnoreList),
            () => config.TreasureChestIgnoreList, value => config.TreasureChestIgnoreList = value);
        NormalizeItemList(report, nameof(config.PreferredBaits),
            () => config.PreferredBaits, value => config.PreferredBaits = value);
        NormalizeItemList(report, nameof(config.PreferredTackles),
            () => config.PreferredTackles, value => config.PreferredTackles = value);
        NormalizeItemList(report, nameof(config.PreferredSecondTackles),
            () => config.PreferredSecondTackles, value => config.PreferredSecondTackles = value);
        NormalizeAssistancePreset(config, report);
        NormalizeDependencies(config, report);

        if (itemCatalog is not null)
            report.Append(NormalizeItems(config, itemCatalog));

        return report;
    }

    private static void NormalizeVersion(ModConfig config, ConfigValidationReport report)
    {
        if (config.ConfigVersion == ModConfig.CurrentVersion)
            return;

        int original = config.ConfigVersion;
        if (original > ModConfig.CurrentVersion)
        {
            report.Warn(nameof(config.ConfigVersion), original,
                "The configuration comes from a newer schema and won't be overwritten.");
            return;
        }

        config.ConfigVersion = ModConfig.CurrentVersion;
        report.Add(nameof(config.ConfigVersion), original, config.ConfigVersion,
            "The configuration was migrated to the current schema.");
    }

    private static void NormalizeKeybind(
        ConfigValidationReport report,
        string property,
        Func<KeybindList?> getValue,
        Action<KeybindList> setValue,
        SButton fallback)
    {
        KeybindList? value = getValue();
        if (value is not null)
            return;

        KeybindList corrected = new(fallback);
        setValue(corrected);
        report.Add(property, null, corrected, "The keybind was missing or invalid.");
    }

    private static void NormalizeEnum<TEnum>(
        ConfigValidationReport report,
        string property,
        Func<TEnum> getValue,
        Action<TEnum> setValue,
        TEnum fallback)
        where TEnum : struct, Enum
    {
        TEnum value = getValue();
        if (Enum.IsDefined(value))
            return;

        setValue(fallback);
        report.Add(property, value, fallback, InvalidEnumReason);
    }

    private static void NormalizeRange(
        ConfigValidationReport report,
        string property,
        Func<int> getValue,
        Action<int> setValue,
        int minimum,
        int maximum)
    {
        int value = getValue();
        int corrected = Math.Clamp(value, minimum, maximum);
        if (value == corrected)
            return;

        setValue(corrected);
        report.Add(property, value, corrected, OutOfRangeReason);
    }

    private static void NormalizeFloatRange(
        ConfigValidationReport report,
        string property,
        Func<float> getValue,
        Action<float> setValue,
        float minimum,
        float maximum)
    {
        float value = getValue();
        float corrected = float.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : minimum;
        if (value.Equals(corrected))
            return;

        setValue(corrected);
        report.Add(property, value, corrected, OutOfRangeReason);
    }

    private static void NormalizeString(
        ConfigValidationReport report,
        string property,
        Func<string?> getValue,
        Action<string> setValue,
        string fallback)
    {
        string? value = getValue();
        string corrected = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (value == corrected)
            return;

        setValue(corrected);
        report.Add(property, value, corrected, "The value was empty or had surrounding whitespace.");
    }

    private static void NormalizeItemList(
        ConfigValidationReport report,
        string property,
        Func<List<string>?> getValue,
        Action<List<string>> setValue)
    {
        List<string>? original = getValue();
        List<string> corrected = original?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        if (original is not null && original.SequenceEqual(corrected, StringComparer.Ordinal))
            return;

        setValue(corrected);
        report.Add(property,
            original is null ? null : string.Join(", ", original),
            string.Join(", ", corrected),
            "Empty and duplicate item IDs were removed.");
    }

    private static void RetireJunkIgnoreList(
        ModConfig config,
        int originalVersion,
        ConfigValidationReport report)
    {
        if (originalVersion > ModConfig.CurrentVersion || config.JunkIgnoreList.Count == 0)
            return;

        HashSet<string> ignored = config.JunkIgnoreList.ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> original = config.JunkList;
        List<string> corrected = original.Where(itemId => !ignored.Contains(itemId)).ToList();
        config.JunkList = corrected;
        config.JunkIgnoreList = [];
        report.Add(nameof(config.JunkIgnoreList), string.Join(", ", ignored), "retired",
            "The obsolete junk ignore list was retired; protected items were removed from the explicit junk list.");
    }


    private static void MigrateCastPowerAdjustmentMode(
        ModConfig config,
        int originalVersion,
        ConfigValidationReport report)
    {
        if (originalVersion >= 18 || originalVersion > ModConfig.CurrentVersion)
            return;

        config.AutomaticCastPowerAdjustmentMode = config.AutomaticCastPowerAdjustment
            ? CastPowerAdjustmentMode.AutomaticOnly
            : CastPowerAdjustmentMode.Off;
        config.AutomaticCastPowerAdjustment = false;
        if (config.AutomaticCastPowerAdjustmentMode == CastPowerAdjustmentMode.AutomaticOnly)
        {
            report.Add(nameof(config.AutomaticCastPowerAdjustment), true,
                config.AutomaticCastPowerAdjustmentMode,
                "The automatic-cast power adjustment toggle was migrated to the automatic-casts mode.");
        }
    }
    private static void MigrateJunkDisposalMode(
        ModConfig config,
        int originalVersion,
        ConfigValidationReport report)
    {
        if (originalVersion >= 14 || originalVersion > ModConfig.CurrentVersion)
            return;

        bool legacyEnabled = config.AutoTrashJunk;
        config.JunkDisposalMode = legacyEnabled
            ? JunkDisposalMode.Immediately
            : JunkDisposalMode.Off;
        config.AutoTrashJunk = false;
        if (legacyEnabled)
        {
            report.Add(nameof(config.AutoTrashJunk), true, config.JunkDisposalMode,
                "The legacy automatic-trash toggle was migrated to the immediate junk disposal mode.");
        }
    }

    private static void MigrateOrderedEquipmentPreferences(
        ModConfig config,
        int originalVersion,
        ConfigValidationReport report)
    {
        if (originalVersion > ModConfig.CurrentVersion)
            return;

        config.PreferredBaits ??= [];
        config.PreferredTackles ??= [];
        config.PreferredSecondTackles ??= [];

        MigratePreference(config.PreferredBait, config.PreferredBaits, nameof(config.PreferredBaits), report);
        MigratePreference(config.PreferredTackle, config.PreferredTackles, nameof(config.PreferredTackles), report);
        MigratePreference(config.PreferredAdvIridiumTackle, config.PreferredSecondTackles,
            nameof(config.PreferredSecondTackles), report);
    }

    private static void MigrateFishDifficultySettings(
        ModConfig config,
        int originalVersion,
        ConfigValidationReport report)
    {
        if (originalVersion > ModConfig.CurrentVersion)
            return;

        bool hadObsoleteCustomization = !config.FishDifficultyMultiplier.Equals(1f)
            || config.FishDifficultyAdditive != 0;
        if (originalVersion < 16)
        {
            config.MinigameAssistance = MinigameAssistancePreset.Off;
            config.FishSpeedPercent = MinigameAssistancePolicy.VanillaPercent;
            config.ProgressGainPercent = MinigameAssistancePolicy.VanillaPercent;
            config.ProgressLossPercent = MinigameAssistancePolicy.VanillaPercent;
            config.TreasureSpeedPercent = MinigameAssistancePolicy.VanillaPercent;
            config.BarSizePercent = MinigameAssistancePolicy.VanillaPercent;
        }

        config.FishDifficultyMultiplier = 1f;
        config.FishDifficultyAdditive = 0;
        if (hadObsoleteCustomization)
        {
            report.Add("FishDifficultyMultiplier/FishDifficultyAdditive", "customized", "retired",
                "The old difficulty controls affected fish behavior as well as speed and couldn't be converted reliably; Minigame Assistance starts at Vanilla.");
        }
    }

    private static void NormalizeAssistancePreset(ModConfig config, ConfigValidationReport report)
    {
        MinigameAssistancePreset configured = config.MinigameAssistance;
        MinigameAssistancePreset detected = MinigameAssistancePresets.Detect(config);
        if (configured == detected)
            return;

        config.MinigameAssistance = detected;
        report.Add(nameof(config.MinigameAssistance), configured, detected,
            "The assistance preset was resolved from its modifier values.");
    }

    private static void MigratePreference(
        string? legacyValue,
        List<string> preferences,
        string property,
        ConfigValidationReport report)
    {
        if (preferences.Count > 0 || string.IsNullOrWhiteSpace(legacyValue)
            || string.Equals(legacyValue.Trim(), "Any", StringComparison.OrdinalIgnoreCase))
            return;

        string migrated = legacyValue.Trim();
        preferences.Add(migrated);
        report.Add(property, legacyValue, migrated,
            "The single-item preference was migrated to an ordered preference list.");
    }

    private static void NormalizeDependencies(ModConfig config, ConfigValidationReport report)
    {
        if (config.SpawnBaitIfDontHave && !config.AutoAttachBait)
        {
            report.Warn(nameof(config.SpawnBaitIfDontHave), config.SpawnBaitIfDontHave,
                $"This setting has no effect while {nameof(config.AutoAttachBait)} is disabled.");
        }

        if (config.SpawnTackleIfDontHave && !config.AutoAttachTackles)
        {
            report.Warn(nameof(config.SpawnTackleIfDontHave), config.SpawnTackleIfDontHave,
                $"This setting has no effect while {nameof(config.AutoAttachTackles)} is disabled.");
        }

        if (config.SkipFishingMiniGame != SkipMinigameBehavior.Off && config.AutoPlayMiniGame)
        {
            report.Warn(nameof(config.AutoPlayMiniGame), config.AutoPlayMiniGame,
                $"{nameof(config.SkipFishingMiniGame)} takes priority when a catch can be skipped.");
        }
    }

    public static ConfigValidationReport NormalizeItems(ModConfig config, IItemCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(catalog);

        ConfigValidationReport report = new();
        NormalizeItemIds(report, nameof(config.PreferredBaits),
            () => config.PreferredBaits, value => config.PreferredBaits = value, catalog, ConfigItemKind.Bait);
        NormalizeItemIds(report, nameof(config.PreferredTackles),
            () => config.PreferredTackles, value => config.PreferredTackles = value, catalog, ConfigItemKind.Tackle);
        NormalizeItemIds(report, nameof(config.PreferredSecondTackles),
            () => config.PreferredSecondTackles, value => config.PreferredSecondTackles = value, catalog,
            ConfigItemKind.Tackle);
        NormalizeItemPreference(report, nameof(config.StartWithFishingRod),
            () => config.StartWithFishingRod, value => config.StartWithFishingRod = value,
            ModConfig.DefaultStarterRod, ConfigItemKind.FishingRod, catalog);

        NormalizeItemIds(report, nameof(config.JunkList),
            () => config.JunkList, value => config.JunkList = value, catalog);
        NormalizeItemIds(report, nameof(config.TreasureChestIgnoreList),
            () => config.TreasureChestIgnoreList, value => config.TreasureChestIgnoreList = value, catalog);

        return report;
    }

    private static void NormalizeItemIds(
        ConfigValidationReport report,
        string property,
        Func<List<string>> getValue,
        Action<List<string>> setValue,
        IItemCatalog catalog,
        ConfigItemKind? expectedKind = null)
    {
        List<string> original = getValue();
        List<string> corrected = original
            .Select(catalog.Find)
            .Where(item => item is not null && (expectedKind is null || item.Kind == expectedKind))
            .Select(item => item!.QualifiedItemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!original.SequenceEqual(corrected, StringComparer.Ordinal))
        {
            setValue(corrected);
            report.Add(property, string.Join(", ", original), string.Join(", ", corrected),
                "Unknown item IDs were removed and known IDs were qualified.");
        }
    }

    private static void NormalizeItemPreference(
        ConfigValidationReport report,
        string property,
        Func<string> getValue,
        Action<string> setValue,
        string sentinel,
        ConfigItemKind expectedKind,
        IItemCatalog catalog)
    {
        string value = getValue();
        if (string.Equals(value, sentinel, StringComparison.OrdinalIgnoreCase))
        {
            if (value != sentinel)
            {
                setValue(sentinel);
                report.Add(property, value, sentinel, "The built-in option name was normalized.");
            }

            return;
        }

        ConfigItem? item = catalog.Find(value);
        string corrected = item?.Kind == expectedKind ? item.QualifiedItemId : sentinel;
        if (value == corrected)
            return;

        setValue(corrected);
        report.Add(property, value, corrected,
            item is null ? "The item ID doesn't exist." : $"The item isn't a supported {expectedKind}.");
    }
}
