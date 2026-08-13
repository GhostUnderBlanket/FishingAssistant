using Microsoft.Xna.Framework.Input;

namespace FishingAssistant.UI;

internal static class ConfigurationMenuGamepadNavigation
{
    public static bool IsDirectional(Buttons button)
    {
        return button is Buttons.DPadUp
            or Buttons.LeftThumbstickUp
            or Buttons.DPadRight
            or Buttons.LeftThumbstickRight
            or Buttons.DPadDown
            or Buttons.LeftThumbstickDown
            or Buttons.DPadLeft
            or Buttons.LeftThumbstickLeft;
    }
}
