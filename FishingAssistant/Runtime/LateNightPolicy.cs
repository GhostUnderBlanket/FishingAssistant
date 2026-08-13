using FishingAssistant.Configuration;

namespace FishingAssistant.Runtime;

internal enum LateNightWarningAction
{
    None,
    Warn,
    WarnAndRequestPause
}

internal sealed record LateNightWarningConditions(
    PauseFishingBehavior Behavior,
    bool AutomationEnabled,
    bool IsFishingContext,
    int CurrentTime,
    int ThresholdTime,
    int WarningsIssued,
    int WarningLimit);

internal sealed record LateNightPauseConditions(
    bool PausePending,
    bool AutomationEnabled,
    bool IsWorldReady,
    bool IsLocalPlayer,
    bool IsRodInUse,
    bool HasBlockingMenu,
    bool HasMinigame,
    bool IsEvent,
    bool IsFestival);

internal static class LateNightPolicy
{
    public static LateNightWarningAction DecideWarning(LateNightWarningConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        int warningLimit = Math.Max(1, conditions.WarningLimit);
        if (conditions.Behavior == PauseFishingBehavior.Off
            || !conditions.AutomationEnabled
            || !conditions.IsFishingContext
            || conditions.CurrentTime < conditions.ThresholdTime
            || conditions.WarningsIssued >= warningLimit)
        {
            return LateNightWarningAction.None;
        }

        bool finalWarning = conditions.WarningsIssued + 1 >= warningLimit;
        return conditions.Behavior == PauseFishingBehavior.WarnAndPause && finalWarning
            ? LateNightWarningAction.WarnAndRequestPause
            : LateNightWarningAction.Warn;
    }

    public static bool ShouldPause(LateNightPauseConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        return conditions.PausePending
            && conditions.AutomationEnabled
            && conditions.IsWorldReady
            && conditions.IsLocalPlayer
            && !conditions.IsRodInUse
            && !conditions.HasBlockingMenu
            && !conditions.HasMinigame
            && !conditions.IsEvent
            && !conditions.IsFestival;
    }
}
