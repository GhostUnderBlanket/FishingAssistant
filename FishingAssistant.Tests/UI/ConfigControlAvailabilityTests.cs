using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class ConfigControlAvailabilityTests
{
    [Fact]
    public void TemporaryEnchantments_AreEnabledWithoutRemotePlayers()
    {
        ConfigControlState state = ConfigControlAvailability.TemporaryEnchantments(false);

        Assert.True(state.IsEnabled);
        Assert.Null(state.UnavailableReasonKey);
    }

    [Fact]
    public void TemporaryEnchantments_AreDisabledForRemotePlayersWithAnExplanation()
    {
        ConfigControlState state = ConfigControlAvailability.TemporaryEnchantments(true);

        Assert.False(state.IsEnabled);
        Assert.Equal("config.unavailable.remote_enchantments", state.UnavailableReasonKey);
    }

    [Fact]
    public void Disabled_RejectsAnEmptyReason()
    {
        Assert.Throws<ArgumentException>(() => ConfigControlState.Disabled(""));
    }
}
