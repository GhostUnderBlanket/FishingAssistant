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

        NormalizeVersion(config, report);
        NormalizeKeybind(report, nameof(config.EnableAutomationButton),
            () => config.EnableAutomationButton, value => config.EnableAutomationButton = value, SButton.F5);
        NormalizeKeybind(report, nameof(config.OpenConfigMenuButton),
            () => config.OpenConfigMenuButton, value => config.OpenConfigMenuButton = value, SButton.F6);

        NormalizeEnum(report, nameof(config.ModStatusPosition),
            () => config.ModStatusPosition, value => config.ModStatusPosition = value, HudPosition.Left);
        NormalizeEnum(report, nameof(config.ActionIfInventoryFull),
            () => config.ActionIfInventoryFull, value => config.ActionIfInventoryFull = value, InventoryFullAction.Stop);
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
        NormalizeFloatRange(report, nameof(config.FishDifficultyMultiplier),
            () => config.FishDifficultyMultiplier, value => config.FishDifficultyMultiplier = value, 0f, 10f);
        NormalizeRange(report, nameof(config.FishDifficultyAdditive),
            () => config.FishDifficultyAdditive, value => config.FishDifficultyAdditive = value, -100, 100);
        NormalizeRange(report, nameof(config.DefaultCastPower),
            () => config.DefaultCastPower, value => config.DefaultCastPower = value, 0, 100);
        NormalizeFloatRange(report, nameof(config.AutoCastDelaySeconds),
            () => config.AutoCastDelaySeconds, value => config.AutoCastDelaySeconds = value, 0f, 10f);
        NormalizeFloatRange(report, nameof(config.UnlockCastPowerTime),
            () => config.UnlockCastPowerTime, value => config.UnlockCastPowerTime = value, 0f, 3f);

        NormalizeString(report, nameof(config.PreferredBait),
            () => config.PreferredBait, value => config.PreferredBait = value, "Any");
        NormalizeString(report, nameof(config.PreferredTackle),
            () => config.PreferredTackle, value => config.PreferredTackle = value, "Any");
        NormalizeString(report, nameof(config.PreferredAdvIridiumTackle),
            () => config.PreferredAdvIridiumTackle, value => config.PreferredAdvIridiumTackle = value, "Any");
        NormalizeString(report, nameof(config.StartWithFishingRod),
            () => config.StartWithFishingRod, value => config.StartWithFishingRod = value,
            ModConfig.DefaultStarterRod);
        NormalizeItemList(report, nameof(config.JunkList),
            () => config.JunkList, value => config.JunkList = value);
        NormalizeItemList(report, nameof(config.JunkIgnoreList),
            () => config.JunkIgnoreList, value => config.JunkIgnoreList = value);
        ResolveJunkListConflicts(config, report);
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

    private static void ResolveJunkListConflicts(ModConfig config, ConfigValidationReport report)
    {
        HashSet<string> ignored = config.JunkIgnoreList.ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> original = config.JunkList;
        List<string> corrected = original.Where(itemId => !ignored.Contains(itemId)).ToList();
        if (original.SequenceEqual(corrected, StringComparer.Ordinal))
            return;

        config.JunkList = corrected;
        report.Add(nameof(config.JunkList), string.Join(", ", original), string.Join(", ", corrected),
            "Items in the ignore list were removed from the junk list.");
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
        NormalizeItemPreference(report, nameof(config.PreferredBait),
            () => config.PreferredBait, value => config.PreferredBait = value,
            "Any", ConfigItemKind.Bait, catalog);
        NormalizeItemPreference(report, nameof(config.PreferredTackle),
            () => config.PreferredTackle, value => config.PreferredTackle = value,
            "Any", ConfigItemKind.Tackle, catalog);
        NormalizeItemPreference(report, nameof(config.PreferredAdvIridiumTackle),
            () => config.PreferredAdvIridiumTackle, value => config.PreferredAdvIridiumTackle = value,
            "Any", ConfigItemKind.Tackle, catalog);
        NormalizeItemPreference(report, nameof(config.StartWithFishingRod),
            () => config.StartWithFishingRod, value => config.StartWithFishingRod = value,
            ModConfig.DefaultStarterRod, ConfigItemKind.FishingRod, catalog);

        NormalizeItemIds(report, nameof(config.JunkList),
            () => config.JunkList, value => config.JunkList = value, catalog);
        NormalizeItemIds(report, nameof(config.JunkIgnoreList),
            () => config.JunkIgnoreList, value => config.JunkIgnoreList = value, catalog);
        ResolveJunkListConflicts(config, report);

        return report;
    }

    private static void NormalizeItemIds(
        ConfigValidationReport report,
        string property,
        Func<List<string>> getValue,
        Action<List<string>> setValue,
        IItemCatalog catalog)
    {
        List<string> original = getValue();
        List<string> corrected = original
            .Select(catalog.Find)
            .Where(item => item is not null)
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
