using FishingAssistant.Debugging;

namespace FishingAssistant.Tests.Debugging;

public sealed class DebugFestivalPolicyTests
{
    [Fact]
    public void CanPrepareFestival_WhenHostIsFreeInTheWorld()
    {
        Assert.True(DebugFestivalPolicy.CanPrepareFestival(
            isWorldReady: true,
            isMainPlayer: true,
            hasActiveEvent: false,
            hasActiveMinigame: false));
    }

    [Theory]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, false, true)]
    public void CanPrepareFestival_WhenPreparationWouldBeUnsafe_ReturnsFalse(
        bool isWorldReady,
        bool isMainPlayer,
        bool hasActiveEvent,
        bool hasActiveMinigame)
    {
        Assert.False(DebugFestivalPolicy.CanPrepareFestival(
            isWorldReady,
            isMainPlayer,
            hasActiveEvent,
            hasActiveMinigame));
    }
}
