using FishingAssistant.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace FishingAssistant.Tests.Configuration;

public sealed class LegacyConfigSerializationTests
{
    private static readonly string[] FishingAssistant2Properties =
    [
        "EnableAutomationButton", "CatchTreasureButton", "OpenConfigMenuButton", "ModStatusPosition",
        "AutoCastFishingRod", "AutoHookFish", "AutoPlayMiniGame", "AutoClosePopup", "AutoLootTreasure",
        "ActionIfInventoryFull", "AutoTrashJunk", "JunkHighestPrice", "AllowTrashFish", "JunkIgnoreList",
        "AutoPauseFishing", "TimeToPause", "WarnCount", "AutoEatFood", "EnergyPercentToEat",
        "AllowEatingFish", "AutoAttachBait", "PreferredBait", "SpawnBaitIfDontHave", "BaitAmountToSpawn",
        "AutoAttachTackles", "PreferredTackle", "PreferredAdvIridiumTackle", "SpawnTackleIfDontHave",
        "SkipFishingMiniGame", "InstantFishBite", "PreferFishAmount", "PreferFishQuality", "AlwaysPerfect",
        "AlwaysMaxFishSize", "FishDifficultyMultiplier", "FishDifficultyAdditive", "InstantCatchTreasure",
        "TreasureChance", "GoldenTreasureChance", "DisplayFishPreview", "ShowFishName", "ShowTreasure",
        "ShowUncaughtFish", "ShowLegendaryFish", "StartWithFishingRod", "DefaultCastPower",
        "UnlockCastPowerTime", "InfiniteBait", "InfiniteTackle", "AddAutoHookEnchantment",
        "AddEfficientEnchantment", "AddMasterEnchantment", "AddPreservingEnchantment",
        "RemoveWhenUnequipped"
    ];

    private static readonly JsonSerializerSettings SmapiCompatibleSettings = CreateSmapiCompatibleSettings();

    [Fact]
    public void LegacyConfig_DeserializesIntoTypedSchema()
    {
        const string json = """
            {
              "EnableAutomationButton": "F5",
              "CatchTreasureButton": "F6",
              "OpenConfigMenuButton": "F7",
              "ModStatusPosition": "Right",
              "ActionIfInventoryFull": "Drop",
              "AutoPauseFishing": "WarnOnly",
              "PreferredBait": "(O)774",
              "SkipFishingMiniGame": "SkipOnlyCaught",
              "PreferFishQuality": "Iridium",
              "TreasureChance": "Always",
              "GoldenTreasureChance": "Never"
            }
            """;

        ModConfig? config = JsonConvert.DeserializeObject<ModConfig>(json, SmapiCompatibleSettings);

        Assert.NotNull(config);
        Assert.Equal("F5", config.EnableAutomationButton.ToString());
        Assert.Equal("F7", config.OpenConfigMenuButton.ToString());
        Assert.Equal(HudPosition.Right, config.ModStatusPosition);
        Assert.Equal(InventoryFullAction.Drop, config.ActionIfInventoryFull);
        Assert.Equal(PauseFishingBehavior.WarnOnly, config.AutoPauseFishing);
        Assert.Equal("(O)774", config.PreferredBait);
        Assert.Equal(SkipMinigameBehavior.SkipOnlyCaught, config.SkipFishingMiniGame);
        Assert.Equal(FishQualityPreference.Iridium, config.PreferFishQuality);
        Assert.Equal(TreasureChanceBehavior.Always, config.TreasureChance);
        Assert.Equal(TreasureChanceBehavior.Never, config.GoldenTreasureChance);
        Assert.False(config.TreasureTargeting);
    }

    [Fact]
    public void TypedEnums_SerializeAsLegacyCompatibleNames()
    {
        ModConfig config = new()
        {
            ModStatusPosition = HudPosition.Right,
            SkipFishingMiniGame = SkipMinigameBehavior.SkipOnlyCaught,
            PreferFishQuality = FishQualityPreference.Gold,
            TreasureTargeting = true
        };

        string json = JsonConvert.SerializeObject(config, SmapiCompatibleSettings);

        Assert.Contains("\"ModStatusPosition\":\"Right\"", json);
        Assert.Contains("\"SkipFishingMiniGame\":\"SkipOnlyCaught\"", json);
        Assert.Contains("\"PreferFishQuality\":\"Gold\"", json);
        Assert.Contains("\"TreasureTargeting\":true", json);
    }

    [Fact]
    public void UnknownEnumName_DoesNotPreventOtherSettingsFromLoading()
    {
        const string json = """
            {
              "ModStatusPosition": "Diagonal",
              "AutoCastFishingRod": false,
              "PreferredBait": "(O)685"
            }
            """;

        ModConfig config = JsonConvert.DeserializeObject<ModConfig>(json, SmapiCompatibleSettings)!;
        ConfigValidationReport report = ConfigValidator.Normalize(config);

        Assert.Equal(HudPosition.Left, config.ModStatusPosition);
        Assert.False(config.AutoCastFishingRod);
        Assert.Equal("(O)685", config.PreferredBait);
        Assert.Contains(report.Corrections,
            correction => correction.Property == nameof(config.ModStatusPosition));
    }

    [Fact]
    public void FullFishingAssistant2Config_PreservesEverySupportedChoice()
    {
        string json = ReadFixture("FishingAssistant2-full-config.json");
        JObject legacy = JObject.Parse(json);
        Assert.Equal(FishingAssistant2Properties.Order(), legacy.Properties().Select(property => property.Name).Order());

        ModConfig config = JsonConvert.DeserializeObject<ModConfig>(json, SmapiCompatibleSettings)!;
        ConfigValidationReport report = ConfigValidator.Normalize(config);
        JObject migrated = JObject.Parse(JsonConvert.SerializeObject(config, SmapiCompatibleSettings));

        foreach (string property in FishingAssistant2Properties.Except(
                     ["CatchTreasureButton", "JunkHighestPrice", "JunkIgnoreList"]))
        {
            Assert.True(JToken.DeepEquals(legacy[property], migrated[property]),
                $"Legacy property '{property}' changed from {legacy[property]} to {migrated[property]}.");
        }

        Assert.Null(migrated["CatchTreasureButton"]);
        Assert.Null(migrated["JunkHighestPrice"]);
        Assert.Null(migrated["JunkIgnoreList"]);
        Assert.Equal(ModConfig.CurrentVersion, migrated[nameof(ModConfig.ConfigVersion)]!.Value<int>());
        Assert.DoesNotContain(report.Warnings, warning =>
            FishingAssistant2Properties.Contains(warning.Property, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void FullFishingAssistant2Config_IdentifiesBothRetiredChoices()
    {
        string json = ReadFixture("FishingAssistant2-full-config.json");

        IReadOnlyList<ConfigPropertySnapshot> retired = ConfigSchemaInspector.FindRetiredProperties(json);

        Assert.Equal(["CatchTreasureButton", "JunkHighestPrice"], retired.Select(property => property.Name));
        Assert.Equal(["\"F2\"", "75"], retired.Select(property => property.DisplayValue));
        Assert.Empty(ConfigSchemaInspector.FindUnknownProperties(json));
    }

    private static string ReadFixture(string name)
    {
        string resourceName = $"FishingAssistant.Tests.Configuration.Fixtures.{name}";
        using Stream stream = typeof(LegacyConfigSerializationTests).Assembly
            .GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{resourceName}'.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private static JsonSerializerSettings CreateSmapiCompatibleSettings()
    {
        Type converterType = typeof(SButton).Assembly.GetType(
            "StardewModdingAPI.Framework.Serialization.KeybindConverter",
            throwOnError: true
        )!;
        JsonConverter keybindConverter = (JsonConverter)Activator.CreateInstance(converterType, nonPublic: true)!;

        return new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = [keybindConverter, new StringEnumConverter()]
        };
    }
}
