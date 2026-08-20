using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class FestivalFishingPolicyTests
{
    [Theory]
    [InlineData(true, 1, true)]
    [InlineData(true, 0, false)]
    [InlineData(false, 120, false)]
    public void IsIceFishingCompetitionActive_RequiresWinter8FestivalAndActiveTimer(
        bool isIceFishingFestival,
        int festivalTimer,
        bool expected)
    {
        Assert.Equal(expected,
            FestivalFishingPolicy.IsIceFishingCompetitionActive(isIceFishingFestival, festivalTimer));
    }
}
