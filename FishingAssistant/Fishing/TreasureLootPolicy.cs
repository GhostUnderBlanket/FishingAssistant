using FishingAssistant.Configuration;

namespace FishingAssistant.Fishing;

internal enum TreasureLootDecision
{
    Reset,
    Wait,
    Collect,
    Close,
    Stop,
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
    bool HasBlockedNonIgnoredItem,
    bool HasIgnoredItem,
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

        if (!conditions.HasBlockedNonIgnoredItem && conditions.HasIgnoredItem)
        {
            return conditions.IgnoredTreasureAction switch
            {
                IgnoredTreasureAction.Drop => TreasureLootDecision.DropIgnored,
                IgnoredTreasureAction.Discard => TreasureLootDecision.DiscardIgnored,
                _ => TreasureLootDecision.KeepIgnoredOpen
            };
        }

        return conditions.InventoryFullAction switch
        {
            InventoryFullAction.Drop => TreasureLootDecision.DropBlocked,
            InventoryFullAction.Discard => TreasureLootDecision.DiscardBlocked,
            _ => TreasureLootDecision.Stop
        };
    }
}
