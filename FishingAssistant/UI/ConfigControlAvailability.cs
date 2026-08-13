namespace FishingAssistant.UI;

internal static class ConfigControlAvailability
{
    public static ConfigControlState TemporaryEnchantments(bool hasRemotePlayers)
    {
        return hasRemotePlayers
            ? ConfigControlState.Disabled("config.unavailable.remote_enchantments")
            : ConfigControlState.Enabled;
    }
}
