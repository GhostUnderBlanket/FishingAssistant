using FishingAssistant.Configuration;

namespace FishingAssistant.Fishing;

internal enum SkipMinigameDecision
{
    Play,
    Skip
}

internal sealed record SkipMinigameConditions(
    SkipMinigameBehavior Behavior,
    bool IsMinigameActive,
    bool FishWasCaughtBefore,
    bool IsFestival,
    bool IsSupportedFishingMinigame);

internal static class SkipMinigamePolicy
{
    public static SkipMinigameDecision Decide(SkipMinigameConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        bool behaviorAllowsSkip = conditions.Behavior switch
        {
            SkipMinigameBehavior.SkipAll => true,
            SkipMinigameBehavior.SkipOnlyCaught => conditions.FishWasCaughtBefore,
            _ => false
        };
        bool shouldSkip = behaviorAllowsSkip
            && conditions.IsMinigameActive
            && (!conditions.IsFestival || conditions.IsSupportedFishingMinigame);
        return shouldSkip ? SkipMinigameDecision.Skip : SkipMinigameDecision.Play;
    }
}
