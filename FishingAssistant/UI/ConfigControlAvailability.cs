namespace FishingAssistant.UI;

internal static class ConfigControlAvailability
{
    public static ConfigControlState Requires(bool enabled, string unavailableReasonKey)
    {
        return enabled
            ? ConfigControlState.Enabled
            : ConfigControlState.Disabled(unavailableReasonKey);
    }

    public static ConfigControlState FishPreviewStyle(bool previewEnabled)
    {
        return previewEnabled
            ? ConfigControlState.Enabled
            : ConfigControlState.Disabled("config.unavailable.fish_preview_style");
    }

    public static ConfigControlState FishName(bool previewEnabled, bool classicStyle)
    {
        if (!previewEnabled)
            return ConfigControlState.Disabled("config.unavailable.fish_preview");

        return classicStyle
            ? ConfigControlState.Enabled
            : ConfigControlState.Disabled("config.info.fish_name_classic_only");
    }

    public static ConfigControlState LegendaryFish(bool previewEnabled, bool revealUncaughtFish)
    {
        if (!previewEnabled)
            return ConfigControlState.Disabled("config.unavailable.fish_preview");

        return revealUncaughtFish
            ? ConfigControlState.Enabled
            : ConfigControlState.Disabled("config.unavailable.uncaught_fish");
    }


    public static ConfigControlState BubbleSteering(bool steeringEnabled)
    {
        return steeringEnabled
            ? ConfigControlState.Enabled
            : ConfigControlState.Disabled("config.unavailable.bubble_steering");
    }
    public static ConfigControlState TemporaryEnchantments(bool hasRemotePlayers)
    {
        return hasRemotePlayers
            ? ConfigControlState.Disabled("config.unavailable.remote_enchantments")
            : ConfigControlState.Enabled;
    }
}
