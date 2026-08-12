namespace FishingAssistant.Configuration;

internal enum HudPosition
{
    Left,
    Right
}

internal enum InventoryFullAction
{
    Stop,
    Drop,
    Discard
}

internal enum PauseFishingBehavior
{
    Off,
    WarnOnly,
    WarnAndPause
}

internal enum SkipMinigameBehavior
{
    Off,
    SkipAll,
    SkipOnlyCaught
}

internal enum FishQualityPreference
{
    Any = -1,
    None = 0,
    Silver = 1,
    Gold = 2,
    Iridium = 4
}

internal enum TreasureChanceBehavior
{
    Default,
    Always,
    Never
}
