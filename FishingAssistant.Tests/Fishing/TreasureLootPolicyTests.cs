using FishingAssistant.Configuration;
using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class TreasureLootPolicyTests
{
    private static TreasureLootConditions SafeConditions => new(
        true, true, true, false, false, true, true, InventoryFullAction.Stop);

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
    [InlineData((int)InventoryFullAction.Drop, (int)TreasureLootDecision.Drop)]
    [InlineData((int)InventoryFullAction.Discard, (int)TreasureLootDecision.Discard)]
    public void Decide_UsesConfiguredFullInventoryAction(int actionValue, int expectedValue)
    {
        TreasureLootConditions conditions = SafeConditions with
        {
            HasUnblockedItem = false,
            InventoryFullAction = (InventoryFullAction)actionValue
        };

        Assert.Equal((TreasureLootDecision)expectedValue, TreasureLootPolicy.Decide(conditions, 0, 0));
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
