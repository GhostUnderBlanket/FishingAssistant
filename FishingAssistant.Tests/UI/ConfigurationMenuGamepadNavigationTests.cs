using FishingAssistant.UI;
using Microsoft.Xna.Framework.Input;

namespace FishingAssistant.Tests.UI;

public sealed class ConfigurationMenuGamepadNavigationTests
{
    [Theory]
    [InlineData(Buttons.DPadUp)]
    [InlineData(Buttons.LeftThumbstickRight)]
    [InlineData(Buttons.DPadDown)]
    [InlineData(Buttons.LeftThumbstickLeft)]
    public void IsDirectional_RecognizesControllerNavigationInput(Buttons button)
    {
        Assert.True(ConfigurationMenuGamepadNavigation.IsDirectional(button));
    }

    [Fact]
    public void IsDirectional_DoesNotTreatActivationAsNavigation()
    {
        Assert.False(ConfigurationMenuGamepadNavigation.IsDirectional(Buttons.A));
    }
}
