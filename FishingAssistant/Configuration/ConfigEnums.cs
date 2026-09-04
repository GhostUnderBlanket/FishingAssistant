using Newtonsoft.Json;

namespace FishingAssistant.Configuration;

[JsonConverter(typeof(SafeStringEnumConverter<HudPosition>))]
internal enum HudPosition
{
    Left,
    Right
}

[JsonConverter(typeof(SafeStringEnumConverter<HudVisibilityMode>))]
internal enum HudVisibilityMode
{
    WhileFishing,
    Always,
    Hidden
}

[JsonConverter(typeof(SafeStringEnumConverter<QuickControlAction>))]
internal enum QuickControlAction
{
    None,
    ToggleAutomation,
    ToggleTreasureTargeting,
    ToggleAutoLootTreasure,
    ToggleAutoEatFood,
    ToggleJunkDisposal,
    ToggleFishPreview,
    ToggleSkipMinigame
}

[JsonConverter(typeof(SafeStringEnumConverter<FishPreviewStyle>))]
internal enum FishPreviewStyle
{
    Classic,
    Sonar
}

[JsonConverter(typeof(SafeStringEnumConverter<SteeringEffort>))]
internal enum SteeringEffort
{
    Low,
    Normal,
    High
}

[JsonConverter(typeof(SafeStringEnumConverter<CastPowerAdjustmentMode>))]
internal enum CastPowerAdjustmentMode
{
    Off,
    AutomaticAndManual,
    AutomaticOnly,
    ManualOnly
}

internal static class CastPowerAdjustmentModeExtensions
{
    public static bool AppliesToAutomatic(this CastPowerAdjustmentMode mode)
        => mode is CastPowerAdjustmentMode.AutomaticAndManual or CastPowerAdjustmentMode.AutomaticOnly;

    public static bool AppliesToManual(this CastPowerAdjustmentMode mode)
        => mode is CastPowerAdjustmentMode.AutomaticAndManual or CastPowerAdjustmentMode.ManualOnly;
}


[JsonConverter(typeof(SafeStringEnumConverter<InventoryFullAction>))]
internal enum InventoryFullAction
{
    Stop,
    Drop,
    Discard
}

[JsonConverter(typeof(SafeStringEnumConverter<IgnoredTreasureAction>))]
internal enum IgnoredTreasureAction
{
    KeepOpen,
    Drop,
    Discard
}

[JsonConverter(typeof(SafeStringEnumConverter<JunkDisposalMode>))]
internal enum JunkDisposalMode
{
    Off,
    WhenInventoryFull,
    Immediately
}

[JsonConverter(typeof(SafeStringEnumConverter<AutomationProfile>))]
internal enum AutomationProfile
{
    Relaxed,
    Training,
    ManualPlus,
    Custom
}

[JsonConverter(typeof(SafeStringEnumConverter<AutomationTimingPreset>))]
internal enum AutomationTimingPreset
{
    Slow,
    Normal,
    Fast,
    Custom
}

[JsonConverter(typeof(SafeStringEnumConverter<MinigameAssistancePreset>))]
internal enum MinigameAssistancePreset
{
    Off,
    Light,
    Comfortable,
    Relaxed,
    Custom
}

[JsonConverter(typeof(SafeStringEnumConverter<PauseFishingBehavior>))]
internal enum PauseFishingBehavior
{
    Off,
    WarnOnly,
    WarnAndPause
}

[JsonConverter(typeof(SafeStringEnumConverter<SkipMinigameBehavior>))]
internal enum SkipMinigameBehavior
{
    Off,
    SkipAll,
    AfterEnoughCatches,
    AfterEnoughPerfectCatches,
    // Kept only so configurations from schema 23 and earlier can be migrated safely.
    SkipOnlyCaught
}

[JsonConverter(typeof(SafeStringEnumConverter<FishQualityPreference>))]
internal enum FishQualityPreference
{
    Any = -1,
    None = 0,
    Silver = 1,
    Gold = 2,
    Iridium = 4
}

[JsonConverter(typeof(SafeStringEnumConverter<FoodFallbackBehavior>))]
internal enum FoodFallbackBehavior
{
    DoNotEat,
    BestValue,
    MostEnergy
}

[JsonConverter(typeof(SafeStringEnumConverter<AutoEatTriggerBehavior>))]
internal enum AutoEatTriggerBehavior
{
    AtEnergyTarget,
    BeforeNextCast
}

[JsonConverter(typeof(SafeStringEnumConverter<FishQualityBehavior>))]
internal enum FishQualityBehavior
{
    Vanilla,
    Minimum,
    Fixed
}

[JsonConverter(typeof(SafeStringEnumConverter<FishAmountBehavior>))]
internal enum FishAmountBehavior
{
    Vanilla,
    Minimum,
    Fixed
}

[JsonConverter(typeof(SafeStringEnumConverter<TreasureChanceBehavior>))]
internal enum TreasureChanceBehavior
{
    Default,
    Always,
    Never
}
