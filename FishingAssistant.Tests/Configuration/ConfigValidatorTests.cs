using FishingAssistant.Configuration;

namespace FishingAssistant.Tests.Configuration;

public sealed class ConfigValidatorTests
{
    [Fact]
    public void Normalize_LeavesDefaultsUnchanged()
    {
        ModConfig config = new();

        ConfigValidationReport report = ConfigValidator.Normalize(config);

        Assert.False(report.WasChanged);
        Assert.Empty(report.Corrections);
    }

    [Fact]
    public void Normalize_CorrectsInvalidAndUnsafeValues()
    {
        ModConfig config = new()
        {
            ConfigVersion = 2,
            EnableAutomationButton = null!,
            ModStatusPosition = (HudPosition)999,
            JunkHighestPrice = -50,
            TimeToPause = 99,
            EnergyPercentToEat = 0,
            BaitAmountToSpawn = 5_000,
            PreferFishAmount = 0,
            FishDifficultyMultiplier = float.NaN,
            FishDifficultyAdditive = 1_000,
            DefaultCastPower = 101,
            UnlockCastPowerTime = -1,
            PreferredBait = "  ",
            PreferredTackle = "  (O)686  ",
            JunkIgnoreList = [" ", "(O)168", "(o)168", " (O)169 "]
        };

        ConfigValidationReport report = ConfigValidator.Normalize(config);

        Assert.True(report.WasChanged);
        Assert.Equal(ModConfig.CurrentVersion, config.ConfigVersion);
        Assert.Equal("F5", config.EnableAutomationButton.ToString());
        Assert.Equal(HudPosition.Left, config.ModStatusPosition);
        Assert.Equal(0, config.JunkHighestPrice);
        Assert.Equal(25, config.TimeToPause);
        Assert.Equal(5, config.EnergyPercentToEat);
        Assert.Equal(999, config.BaitAmountToSpawn);
        Assert.Equal(1, config.PreferFishAmount);
        Assert.Equal(0f, config.FishDifficultyMultiplier);
        Assert.Equal(100, config.FishDifficultyAdditive);
        Assert.Equal(100, config.DefaultCastPower);
        Assert.Equal(0f, config.UnlockCastPowerTime);
        Assert.Equal("Any", config.PreferredBait);
        Assert.Equal("(O)686", config.PreferredTackle);
        Assert.Equal(["(O)168", "(O)169"], config.JunkIgnoreList);
        Assert.Contains(report.Corrections, correction => correction.Property == nameof(config.ConfigVersion));
        Assert.Contains(report.Corrections, correction => correction.Property == nameof(config.EnableAutomationButton));
    }

    [Fact]
    public void Normalize_DoesNotDowngradeFutureSchema()
    {
        ModConfig config = new()
        {
            ConfigVersion = ModConfig.CurrentVersion + 1
        };

        ConfigValidationReport report = ConfigValidator.Normalize(config);

        Assert.Equal(ModConfig.CurrentVersion + 1, config.ConfigVersion);
        Assert.False(report.WasChanged);
        Assert.Single(report.Warnings);
        Assert.Equal(nameof(config.ConfigVersion), report.Warnings[0].Property);
    }

    [Fact]
    public void Normalize_ReportsInactiveAndOverriddenSettings()
    {
        ModConfig config = new()
        {
            AutoAttachBait = false,
            SpawnBaitIfDontHave = true,
            AutoAttachTackles = false,
            SpawnTackleIfDontHave = true,
            AutoPlayMiniGame = true,
            SkipFishingMiniGame = SkipMinigameBehavior.SkipAll
        };

        ConfigValidationReport report = ConfigValidator.Normalize(config);

        Assert.False(report.WasChanged);
        Assert.Equal(3, report.Warnings.Count);
        Assert.Contains(report.Warnings, warning => warning.Property == nameof(config.SpawnBaitIfDontHave));
        Assert.Contains(report.Warnings, warning => warning.Property == nameof(config.SpawnTackleIfDontHave));
        Assert.Contains(report.Warnings, warning => warning.Property == nameof(config.AutoPlayMiniGame));
    }

    [Fact]
    public void Normalize_ValidatesAndQualifiesItemSettings()
    {
        ModConfig config = new()
        {
            PreferredBait = "685",
            PreferredTackle = "(O)WrongCategory",
            PreferredAdvIridiumTackle = "missing",
            StartWithFishingRod = "TrainingRod",
            JunkIgnoreList = ["168", "missing", "(O)168"]
        };
        FakeItemCatalog catalog = new(
            new ConfigItem("(O)685", ConfigItemKind.Bait),
            new ConfigItem("(O)WrongCategory", ConfigItemKind.Other),
            new ConfigItem("(T)TrainingRod", ConfigItemKind.FishingRod),
            new ConfigItem("(O)168", ConfigItemKind.Other)
        );

        ConfigValidationReport report = ConfigValidator.Normalize(config, catalog);

        Assert.Equal("(O)685", config.PreferredBait);
        Assert.Equal("Any", config.PreferredTackle);
        Assert.Equal("Any", config.PreferredAdvIridiumTackle);
        Assert.Equal("(T)TrainingRod", config.StartWithFishingRod);
        Assert.Equal(["(O)168"], config.JunkIgnoreList);
        Assert.Equal(5, report.Corrections.Count);
    }

    private sealed class FakeItemCatalog(params ConfigItem[] items) : IItemCatalog
    {
        private readonly Dictionary<string, ConfigItem> items = items
            .SelectMany(item => new[]
            {
                KeyValuePair.Create(item.QualifiedItemId, item),
                KeyValuePair.Create(item.QualifiedItemId[(item.QualifiedItemId.IndexOf(')') + 1)..], item)
            })
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        public ConfigItem? Find(string itemId)
        {
            return this.items.GetValueOrDefault(itemId);
        }
    }
}
