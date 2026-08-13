using FishingAssistant.UI;
using StardewModdingAPI;

namespace FishingAssistant.Tests.UI;

public sealed class ConfigurationMenuInputTests
{
    [Fact]
    public void IsOpenRequested_AcceptsTheConfiguredKeybind()
    {
        Assert.True(ConfigurationMenuInput.IsOpenRequested(true, []));
    }

    [Fact]
    public void IsOpenRequested_AcceptsTheControllerFallback()
    {
        Assert.True(ConfigurationMenuInput.IsOpenRequested(false,
            [ConfigurationMenuInput.ControllerFallbackButton]));
    }

    [Fact]
    public void IsOpenRequested_RejectsUnrelatedInput()
    {
        Assert.False(ConfigurationMenuInput.IsOpenRequested(false, [SButton.ControllerA]));
    }
}
