using Newtonsoft.Json;

namespace FishingAssistant.Configuration;

[JsonConverter(typeof(SafeStringEnumConverter<HudPosition>))]
internal enum HudPosition
{
    Left,
    Right
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

[JsonConverter(typeof(SafeStringEnumConverter<AutomationProfile>))]
internal enum AutomationProfile
{
    Relaxed,
    Training,
    ManualPlus,
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

[JsonConverter(typeof(SafeStringEnumConverter<TreasureChanceBehavior>))]
internal enum TreasureChanceBehavior
{
    Default,
    Always,
    Never
}
