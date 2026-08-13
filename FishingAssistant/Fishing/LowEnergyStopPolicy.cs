using FishingAssistant.Runtime;

namespace FishingAssistant.Fishing;

internal enum LowEnergyStopDecision
{
    None,
    StopBeforeExhaustion,
    StopAtEatingThreshold
}

internal sealed record LowEnergyStopConditions(
    bool AutomationEnabled,
    bool AutoCastEnabled,
    AutomationState State,
    bool CastConsumesStamina,
    bool PlayerIsEating,
    float Stamina,
    float MaxStamina,
    float CastStaminaCost,
    bool AutoEatEnabled,
    int EatingThresholdPercent);

internal static class LowEnergyStopPolicy
{
    public static LowEnergyStopDecision Decide(LowEnergyStopConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (!conditions.AutomationEnabled
            || !conditions.AutoCastEnabled
            || conditions.State != AutomationState.Ready
            || !conditions.CastConsumesStamina
            || conditions.PlayerIsEating)
        {
            return LowEnergyStopDecision.None;
        }

        if (conditions.AutoEatEnabled
            && conditions.MaxStamina > 0f
            && conditions.Stamina <= conditions.MaxStamina * conditions.EatingThresholdPercent / 100f)
        {
            return LowEnergyStopDecision.StopAtEatingThreshold;
        }

        return conditions.Stamina <= Math.Max(0f, conditions.CastStaminaCost)
            ? LowEnergyStopDecision.StopBeforeExhaustion
            : LowEnergyStopDecision.None;
    }
}
