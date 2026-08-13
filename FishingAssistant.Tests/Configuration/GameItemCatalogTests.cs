using FishingAssistant.Configuration;

namespace FishingAssistant.Tests.Configuration;

public sealed class GameItemCatalogTests
{
    [Fact]
    public void SupportedStarterRods_ContainsEveryVanillaFishingRod()
    {
        string[] expected =
        [
            "(T)TrainingRod",
            "(T)BambooPole",
            "(T)FiberglassRod",
            "(T)IridiumRod",
            "(T)AdvancedIridiumRod"
        ];

        Assert.Equal(expected.Order(), GameItemCatalog.SupportedStarterRods.Order());
    }
}
