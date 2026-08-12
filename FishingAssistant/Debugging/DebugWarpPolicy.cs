namespace FishingAssistant.Debugging;

internal static class DebugWarpPolicy
{
    public static bool CanWarp(bool isWorldReady, bool hasActiveEvent, bool hasActiveMinigame)
    {
        return isWorldReady && !hasActiveEvent && !hasActiveMinigame;
    }
}
