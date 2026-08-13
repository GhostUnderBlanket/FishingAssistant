namespace FishingAssistant.Configuration;

internal static class AutomationProfiles
{
    public static void Apply(ModConfig config, AutomationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.AutomationProfile = profile;
        switch (profile)
        {
            case AutomationProfile.Relaxed:
                SetCore(config, autoCast: true, autoHook: true, autoMinigame: true,
                    autoClose: true, autoLoot: true, bubbleSteering: false);
                break;
            case AutomationProfile.Training:
                SetCore(config, autoCast: true, autoHook: true, autoMinigame: false,
                    autoClose: true, autoLoot: true, bubbleSteering: false);
                break;
            case AutomationProfile.ManualPlus:
                SetCore(config, autoCast: false, autoHook: false, autoMinigame: false,
                    autoClose: true, autoLoot: true, bubbleSteering: true);
                break;
        }
    }

    public static void MarkCustom(ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.AutomationProfile = AutomationProfile.Custom;
    }

    private static void SetCore(
        ModConfig config,
        bool autoCast,
        bool autoHook,
        bool autoMinigame,
        bool autoClose,
        bool autoLoot,
        bool bubbleSteering)
    {
        config.AutoCastFishingRod = autoCast;
        config.AutoHookFish = autoHook;
        config.AutoPlayMiniGame = autoMinigame;
        config.AutoClosePopup = autoClose;
        config.AutoLootTreasure = autoLoot;
        config.AutomaticBubbleSteering = bubbleSteering;
        config.DisplayFishPreview = true;
    }
}
