namespace FishingAssistant.Runtime;

internal sealed record FishingObservation(
    bool IsEnabled,
    bool IsWorldReady,
    bool HasFishingRod,
    bool HasBlockingMenu = false,
    bool IsTimingCast = false,
    bool IsCasting = false,
    bool IsBobberInAir = false,
    bool IsFishing = false,
    bool IsNibbling = false,
    bool IsReeling = false,
    bool IsFishCaught = false,
    bool IsPullingOutOfWater = false,
    bool IsMinigame = false,
    bool IsTreasureMenu = false);
