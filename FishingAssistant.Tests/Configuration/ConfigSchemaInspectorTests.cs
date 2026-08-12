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
              "RemovedLegacyOption": 42,
              "FutureOption": "value"
            }
            """;

        IReadOnlyList<string> properties = ConfigSchemaInspector.FindUnknownProperties(json);

        Assert.Equal(["FutureOption", "RemovedLegacyOption"], properties);
    }
}
