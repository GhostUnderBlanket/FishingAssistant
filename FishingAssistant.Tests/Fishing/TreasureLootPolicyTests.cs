using FishingAssistant.Configuration;
using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class TreasureLootPolicyTests
{
    private static TreasureLootConditions SafeConditions => new(
        true, true, true, false, false, true, true, false, false,
        InventoryFullAction.Stop, IgnoredTreasureAction.KeepOpen);

    [Fact]
    public void Decide_WaitsForInitialViewingDelay()
    {
        Assert.Equal(TreasureLootDecision.Wait,
            TreasureLootPolicy.Decide(SafeConditions, TreasureLootPolicy.InitialDelayTicks - 1,
                TreasureLootPolicy.InitialDelayTicks));
        Assert.Equal(TreasureLootDecision.Collect,
            TreasureLootPolicy.Decide(SafeConditions, TreasureLootPolicy.InitialDelayTicks,
                TreasureLootPolicy.InitialDelayTicks));
    }

    [Fact]
    public void Decide_ClosesAnEmptyTreasureMenu()
    {
        TreasureLootConditions conditions = SafeConditions with { HasRemainingItems = false };

        Assert.Equal(TreasureLootDecision.Close,
            TreasureLootPolicy.Decide(conditions, 0, 0));
    }

    [Theory]
    [InlineData((int)InventoryFullAction.Stop, (int)TreasureLootDecision.Stop)]
    [InlineData((int)InventoryFullAction.Drop, (int)TreasureLootDecision.DropBlocked)]
    [InlineData((int)InventoryFullAction.Discard, (int)TreasureLootDecision.DiscardBlocked)]
    public void Decide_UsesConfiguredFullInventoryAction(int actionValue, int expectedValue)
    {
        TreasureLootConditions conditions = SafeConditions with
        {
            HasCollectibleItem = false,
            HasBlockedNonIgnoredItem = true,
            InventoryFullAction = (InventoryFullAction)actionValue
        };

        Assert.Equal((TreasureLootDecision)expectedValue, TreasureLootPolicy.Decide(conditions, 0, 0));
    }

    [Theory]
    [InlineData((int)IgnoredTreasureAction.KeepOpen, (int)TreasureLootDecision.KeepIgnoredOpen)]
    [InlineData((int)IgnoredTreasureAction.Drop, (int)TreasureLootDecision.DropIgnored)]
    [InlineData((int)IgnoredTreasureAction.Discard, (int)TreasureLootDecision.DiscardIgnored)]
    public void Decide_UsesConfiguredActionWhenOnlyIgnoredTreasureRemains(int actionValue, int expectedValue)
    {
        TreasureLootConditions conditions = SafeConditions with
        {
            HasCollectibleItem = false,
            HasIgnoredItem = true,
            IgnoredTreasureAction = (IgnoredTreasureAction)actionValue
        };

        Assert.Equal((TreasureLootDecision)expectedValue, TreasureLootPolicy.Decide(conditions, 0, 0));
    }

    [Fact]
    public void Decide_HandlesBlockedNonIgnoredTreasureBeforeIgnoredTreasure()
    {
        TreasureLootConditions conditions = SafeConditions with
        {
            HasCollectibleItem = false,
            HasBlockedNonIgnoredItem = true,
            HasIgnoredItem = true,
            InventoryFullAction = InventoryFullAction.Drop,
            IgnoredTreasureAction = IgnoredTreasureAction.Discard
        };

        Assert.Equal(TreasureLootDecision.DropBlocked,
            TreasureLootPolicy.Decide(conditions, 0, 0));
    }

    [Theory]
    [InlineData(false, true, true, false, false)]
    [InlineData(true, false, true, false, false)]
    [InlineData(true, true, false, false, false)]
    [InlineData(true, true, true, true, false)]
    [InlineData(true, true, true, false, true)]
    public void Decide_WaitsWhenDisabledOrPlayerIsInteracting(
        bool automationEnabled,
        bool autoLootEnabled,
        bool isMenu,
        bool holdingItem,
        bool stopped)
    {
        TreasureLootConditions conditions = SafeConditions with
        {
            AutomationEnabled = automationEnabled,
            AutoLootEnabled = autoLootEnabled,
            IsFishingTreasureMenu = isMenu,
            IsPlayerHoldingItem = holdingItem,
            CollectionStopped = stopped
        };

        TreasureLootDecision expected = isMenu ? TreasureLootDecision.Wait : TreasureLootDecision.Reset;
        Assert.Equal(expected, TreasureLootPolicy.Decide(conditions, 0, 0));
    }
}
