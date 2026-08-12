using FishingAssistant.Debugging;

namespace FishingAssistant.Tests.Debugging;

public sealed class DebugWarpPolicyTests
{
    [Fact]
    public void CanWarp_WhenWorldIsReadyAndPlayerIsNotBusy()
    {
        Assert.True(DebugWarpPolicy.CanWarp(
            isWorldReady: true,
            hasActiveEvent: false,
            hasActiveMinigame: false));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void CanWarp_WhenWorldIsUnavailableOrPlayerIsBusy_ReturnsFalse(
        bool isWorldReady,
        bool hasActiveEvent,
        bool hasActiveMinigame)
    {
        Assert.False(DebugWarpPolicy.CanWarp(isWorldReady, hasActiveEvent, hasActiveMinigame));
    }
}
