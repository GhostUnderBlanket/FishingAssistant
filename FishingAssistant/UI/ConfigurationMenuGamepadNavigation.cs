using Microsoft.Xna.Framework.Input;

namespace FishingAssistant.UI;

internal static class ConfigurationMenuGamepadNavigation
{
    public static int? GetManualDirection(Buttons button, bool snappyMenusEnabled)
    {
        if (snappyMenusEnabled)
            return null;

        return button switch
        {
            Buttons.DPadUp or Buttons.LeftThumbstickUp => 0,
            Buttons.DPadRight or Buttons.LeftThumbstickRight => 1,
            Buttons.DPadDown or Buttons.LeftThumbstickDown => 2,
            Buttons.DPadLeft or Buttons.LeftThumbstickLeft => 3,
            _ => null
        };
    }
}
