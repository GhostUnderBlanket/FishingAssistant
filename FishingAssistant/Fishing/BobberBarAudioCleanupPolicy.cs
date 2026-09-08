namespace FishingAssistant.Fishing;

internal static class BobberBarAudioCleanupPolicy
{
    public static bool ShouldStopForCompletion(bool hasOtherActiveLocalBobberBar)
    {
        return !hasOtherActiveLocalBobberBar;
    }

    public static bool ShouldStopOrphaned(bool hasAnyActiveLocalBobberBar)
    {
        return !hasAnyActiveLocalBobberBar;
    }
}
