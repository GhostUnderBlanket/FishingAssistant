using StardewModdingAPI;

namespace FishingAssistant.UI;

internal static class ConfigurationMenuInput
{
    public const SButton ControllerFallbackButton = SButton.ControllerBack;

    public static bool IsOpenRequested(bool configuredKeybindPressed, IEnumerable<SButton> pressedButtons)
    {
        ArgumentNullException.ThrowIfNull(pressedButtons);
        return configuredKeybindPressed || pressedButtons.Contains(ControllerFallbackButton);
    }
}
