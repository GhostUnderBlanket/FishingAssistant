namespace FishingAssistant.UI;

internal static class ConfigControlAvailability
{
    public static ConfigControlState FishPreviewStyle(bool previewEnabled)
    {
        return previewEnabled
            ? ConfigControlState.Enabled
            : ConfigControlState.Disabled("config.unavailable.fish_preview_style");
    }

    public static ConfigControlState TemporaryEnchantments(bool hasRemotePlayers)
    {
        return hasRemotePlayers
            ? ConfigControlState.Disabled("config.unavailable.remote_enchantments")
            : ConfigControlState.Enabled;
    }
}
