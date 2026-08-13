using Microsoft.Xna.Framework.Input;

namespace FishingAssistant.UI;

internal static class CategoryNavigationInput
{
    public static int? GetDirection(Buttons button)
    {
        return button switch
        {
            Buttons.LeftShoulder or Buttons.LeftTrigger => -1,
            Buttons.RightShoulder or Buttons.RightTrigger => 1,
            _ => null
        };
    }
}
