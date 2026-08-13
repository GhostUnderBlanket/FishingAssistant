using FishingAssistant.UI;
using Microsoft.Xna.Framework.Input;

namespace FishingAssistant.Tests.UI;

public sealed class ConfigurationMenuGamepadNavigationTests
{
    [Theory]
    [InlineData(Buttons.DPadUp, 0)]
    [InlineData(Buttons.DPadRight, 1)]
    [InlineData(Buttons.DPadDown, 2)]
    [InlineData(Buttons.DPadLeft, 3)]
    public void GetManualDirection_MapsDirectionalInputWithoutSnappyMenus(Buttons button, int expectedDirection)
    {
        Assert.Equal(expectedDirection, ConfigurationMenuGamepadNavigation.GetManualDirection(button, false));
    }

    [Fact]
    public void GetManualDirection_DefersToVanillaSnappyNavigation()
    {
        Assert.Null(ConfigurationMenuGamepadNavigation.GetManualDirection(Buttons.DPadDown, true));
    }
}
