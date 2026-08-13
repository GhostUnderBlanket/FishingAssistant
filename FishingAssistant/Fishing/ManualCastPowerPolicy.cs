namespace FishingAssistant.Fishing;

internal enum ManualCastPowerDecision
{
    Reset,
    UseVanilla,
    HoldConfiguredPower
}

internal static class ManualCastPowerPolicy
{
    public const float NeverUnlockSeconds = 3f;

    public static ManualCastPowerDecision Decide(
        bool isTimingCast,
        bool isAutomaticCast,
        int elapsedTicks,
        float unlockSeconds)
    {
        if (!isTimingCast || isAutomaticCast)
            return ManualCastPowerDecision.Reset;

        float seconds = Math.Clamp(unlockSeconds, 0f, NeverUnlockSeconds);
        if (seconds <= 0f)
            return ManualCastPowerDecision.UseVanilla;
        if (seconds >= NeverUnlockSeconds)
            return ManualCastPowerDecision.HoldConfiguredPower;

        int requiredTicks = (int)Math.Ceiling(seconds * 60f);
        return elapsedTicks < requiredTicks
            ? ManualCastPowerDecision.HoldConfiguredPower
            : ManualCastPowerDecision.UseVanilla;
    }
}
