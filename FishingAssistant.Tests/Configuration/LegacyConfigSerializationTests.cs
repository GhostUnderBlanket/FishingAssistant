using FishingAssistant.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StardewModdingAPI;

namespace FishingAssistant.Tests.Configuration;

public sealed class LegacyConfigSerializationTests
{
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
        Assert.Equal("F6", config.CatchTreasureButton.ToString());
        Assert.Equal("F7", config.OpenConfigMenuButton.ToString());
        Assert.Equal(HudPosition.Right, config.ModStatusPosition);
        Assert.Equal(InventoryFullAction.Drop, config.ActionIfInventoryFull);
        Assert.Equal(PauseFishingBehavior.WarnOnly, config.AutoPauseFishing);
        Assert.Equal("(O)774", config.PreferredBait);
        Assert.Equal(SkipMinigameBehavior.SkipOnlyCaught, config.SkipFishingMiniGame);
        Assert.Equal(FishQualityPreference.Iridium, config.PreferFishQuality);
        Assert.Equal(TreasureChanceBehavior.Always, config.TreasureChance);
        Assert.Equal(TreasureChanceBehavior.Never, config.GoldenTreasureChance);
    }

    [Fact]
    public void TypedEnums_SerializeAsLegacyCompatibleNames()
    {
        ModConfig config = new()
        {
            ModStatusPosition = HudPosition.Right,
            SkipFishingMiniGame = SkipMinigameBehavior.SkipOnlyCaught,
            PreferFishQuality = FishQualityPreference.Gold
        };

        string json = JsonConvert.SerializeObject(config, SmapiCompatibleSettings);

        Assert.Contains("\"ModStatusPosition\":\"Right\"", json);
        Assert.Contains("\"SkipFishingMiniGame\":\"SkipOnlyCaught\"", json);
        Assert.Contains("\"PreferFishQuality\":\"Gold\"", json);
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
