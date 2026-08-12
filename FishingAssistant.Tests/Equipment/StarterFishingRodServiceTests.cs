using FishingAssistant.Equipment;

namespace FishingAssistant.Tests.Equipment;

public sealed class StarterFishingRodServiceTests
{
    [Fact]
    public void TestRod_IsTheHighestLevelVanillaFishingRod()
    {
        Assert.Equal("(T)AdvancedIridiumRod", StarterFishingRodService.TestRodItemId);
    }
}
