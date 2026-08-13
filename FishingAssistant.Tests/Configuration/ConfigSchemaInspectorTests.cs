using FishingAssistant.Configuration;

namespace FishingAssistant.Tests.Configuration;

public sealed class ConfigSchemaInspectorTests
{
    [Fact]
    public void IsLegacyJson_ReturnsTrueWhenVersionIsMissing()
    {
        const string json = """
            {
              "EnableAutomationButton": "F5",
              "AutoCastFishingRod": true
            }
            """;

        Assert.True(ConfigSchemaInspector.IsLegacyJson(json));
    }

    [Fact]
    public void IsLegacyJson_ReturnsFalseWhenVersionIsPresent()
    {
        const string json = """
            {
              "ConfigVersion": 3,
              "EnableAutomationButton": "F5"
            }
            """;

        Assert.False(ConfigSchemaInspector.IsLegacyJson(json));
    }

    [Fact]
    public void FindUnknownProperties_ReportsOnlyUnknownTopLevelKeys()
    {
        const string json = """
            {
              "ConfigVersion": 2,
              "AutoCastFishingRod": true,
              "JunkHighestPrice": 50,
              "CatchTreasureButton": "F6",
              "RemovedLegacyOption": 42,
              "FutureOption": "value"
            }
            """;

        IReadOnlyList<ConfigPropertySnapshot> properties = ConfigSchemaInspector.FindUnknownProperties(json);

        Assert.Equal(["FutureOption", "RemovedLegacyOption"], properties.Select(property => property.Name));
        Assert.Equal(["\"value\"", "42"], properties.Select(property => property.DisplayValue));
    }

    [Fact]
    public void FindRetiredProperties_ReportsLegacyValuesSeparately()
    {
        const string json = """
            {
              "JunkHighestPrice": 75,
              "CatchTreasureButton": "F6",
              "AutoCastFishingRod": true
            }
            """;

        IReadOnlyList<ConfigPropertySnapshot> properties = ConfigSchemaInspector.FindRetiredProperties(json);

        Assert.Collection(properties,
            property => Assert.Equal(new ConfigPropertySnapshot("CatchTreasureButton", "\"F6\""), property),
            property => Assert.Equal(new ConfigPropertySnapshot("JunkHighestPrice", "75"), property));
    }

    [Fact]
    public void FindUnknownProperties_RedactsSensitiveAndBoundsLongValues()
    {
        string json = $$"""
            {
              "ApiKey": "do-not-log-this",
              "LargeLegacyValue": "{{new string('x', 200)}}"
            }
            """;

        IReadOnlyList<ConfigPropertySnapshot> properties = ConfigSchemaInspector.FindUnknownProperties(json);

        Assert.Equal("[redacted]", properties.Single(property => property.Name == "ApiKey").DisplayValue);
        string bounded = properties.Single(property => property.Name == "LargeLegacyValue").DisplayValue;
        Assert.EndsWith("…", bounded);
        Assert.True(bounded.Length <= 121);
    }
}
