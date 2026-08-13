namespace FishingAssistant.Fishing;

internal enum ManualCastPowerDecision
{
    Reset,
    UseVanilla,
    HoldSessionPower,
    RememberVanillaPower
}

internal sealed record ManualCastPowerConditions(
    bool AutomationEnabled,
    bool IsTimingCast,
    bool WasTimingManualCast,
    bool PlayerInputObserved,
    bool IsAutomaticCast,
    bool WasUnlocked,
    int ElapsedTicks,
    float UnlockSeconds);

internal static class ManualCastPowerPolicy
{
    public const float NeverUnlockSeconds = 3f;

    public static ManualCastPowerDecision Decide(
        ManualCastPowerConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        if (!conditions.AutomationEnabled)
            return ManualCastPowerDecision.Reset;
        if (conditions.IsAutomaticCast)
            return ManualCastPowerDecision.Reset;

        if (!conditions.IsTimingCast)
            return conditions.WasTimingManualCast
                && conditions.PlayerInputObserved
                && conditions.WasUnlocked
                ? ManualCastPowerDecision.RememberVanillaPower
                : ManualCastPowerDecision.Reset;

        float seconds = Math.Clamp(conditions.UnlockSeconds, 0f, NeverUnlockSeconds);
        if (seconds <= 0f)
            return ManualCastPowerDecision.UseVanilla;
        if (seconds >= NeverUnlockSeconds)
            return ManualCastPowerDecision.HoldSessionPower;

        int requiredTicks = (int)Math.Ceiling(seconds * 60f);
        return conditions.ElapsedTicks < requiredTicks
            ? ManualCastPowerDecision.HoldSessionPower
            : ManualCastPowerDecision.UseVanilla;
    }
}
