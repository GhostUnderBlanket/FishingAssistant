using FishingAssistant.Configuration;

namespace FishingAssistant.Fishing;

internal enum TreasureLootDecision
{
    Reset,
    Wait,
    Collect,
    Close,
    Stop,
    StopForProtectedItem,
    DropBlocked,
    DiscardBlocked,
    KeepIgnoredOpen,
    DropIgnored,
    DiscardIgnored
}

internal sealed record TreasureLootConditions(
    bool AutomationEnabled,
    bool AutoLootEnabled,
    bool IsFishingTreasureMenu,
    bool IsPlayerHoldingItem,
    bool CollectionStopped,
    bool HasRemainingItems,
    bool HasCollectibleItem,
    bool HasBlockedRewardItem,
    bool HasBlockedProtectedItem,
    bool HasIgnoredRewardItem,
    InventoryFullAction InventoryFullAction,
    IgnoredTreasureAction IgnoredTreasureAction)
{
    public bool IsEligible => AutomationEnabled
        && AutoLootEnabled
        && IsFishingTreasureMenu
        && !IsPlayerHoldingItem
        && !CollectionStopped;
}

internal static class TreasureLootPolicy
{
    public const int InitialDelayTicks = 30;
    public const int ItemDelayTicks = 6;

    public static TreasureLootDecision Decide(
        TreasureLootConditions conditions,
        int elapsedTicks,
        int requiredTicks)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (!conditions.IsFishingTreasureMenu)
            return TreasureLootDecision.Reset;
        if (!conditions.IsEligible || elapsedTicks < Math.Max(0, requiredTicks))
            return TreasureLootDecision.Wait;
        if (!conditions.HasRemainingItems)
            return TreasureLootDecision.Close;
        if (conditions.HasCollectibleItem)
            return TreasureLootDecision.Collect;

        if (conditions.HasBlockedRewardItem)
        {
            return conditions.InventoryFullAction switch
            {
                InventoryFullAction.Drop => TreasureLootDecision.DropBlocked,
                InventoryFullAction.Discard => TreasureLootDecision.DiscardBlocked,
                _ => TreasureLootDecision.Stop
            };
        }

        if (conditions.HasIgnoredRewardItem)
        {
            if (conditions.IgnoredTreasureAction == IgnoredTreasureAction.Drop)
                return TreasureLootDecision.DropIgnored;
            if (conditions.IgnoredTreasureAction == IgnoredTreasureAction.Discard)
                return TreasureLootDecision.DiscardIgnored;
        }

        if (conditions.HasBlockedProtectedItem)
            return TreasureLootDecision.StopForProtectedItem;

        return conditions.HasIgnoredRewardItem
            ? TreasureLootDecision.KeepIgnoredOpen
            : TreasureLootDecision.Stop;
    }
}
