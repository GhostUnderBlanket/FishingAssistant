using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class ConfigControlAvailabilityTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void FishPreviewStyle_RequiresPreview(bool previewEnabled, bool expectedEnabled)
    {
        ConfigControlState state = ConfigControlAvailability.FishPreviewStyle(previewEnabled);

        Assert.Equal(expectedEnabled, state.IsEnabled);
        Assert.Equal(expectedEnabled ? null : "config.unavailable.fish_preview_style",
            state.UnavailableReasonKey);
    }

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
