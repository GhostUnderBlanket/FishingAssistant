using FishingAssistant.Runtime;

namespace FishingAssistant.Fishing;

internal sealed record MinigameControlConditions(
    bool AutomationEnabled,
    bool AutoPlayEnabled,
    AutomationState State,
    bool IsActive,
    float TargetPosition,
    float BarPosition,
    int BarHeight);

internal sealed record MinigameControlDecision(bool ShouldControl, float BarSpeed)
{
    public static MinigameControlDecision Inactive { get; } = new(false, 0f);
}

internal static class MinigameControlPolicy
{
    private const float FishCenterOffset = 30f;
    private const float ProportionalGain = 0.45f;
    private const float MaximumSpeed = 16f;
    private const float DeadZone = 2f;

    public static MinigameControlDecision Decide(MinigameControlConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (!conditions.AutomationEnabled
            || !conditions.AutoPlayEnabled
            || conditions.State != AutomationState.Minigame
            || !conditions.IsActive
            || conditions.BarHeight <= 0)
        {
            return MinigameControlDecision.Inactive;
        }

        float targetCenter = conditions.TargetPosition + FishCenterOffset;
        float currentCenter = conditions.BarPosition + conditions.BarHeight / 2f;
        float error = targetCenter - currentCenter;
        float speed = Math.Abs(error) <= DeadZone
            ? 0f
            : Math.Clamp(error * ProportionalGain, -MaximumSpeed, MaximumSpeed);
        return new MinigameControlDecision(true, speed);
    }
}
