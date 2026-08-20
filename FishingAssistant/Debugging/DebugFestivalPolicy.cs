namespace FishingAssistant.Debugging;

internal static class DebugFestivalPolicy
{
    public static bool CanPrepareFestival(
        bool isWorldReady,
        bool isMainPlayer,
        bool hasActiveEvent,
        bool hasActiveMinigame)
    {
        return isWorldReady && isMainPlayer && !hasActiveEvent && !hasActiveMinigame;
    }
}
