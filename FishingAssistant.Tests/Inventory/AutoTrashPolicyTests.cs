using FishingAssistant.Inventory;

namespace FishingAssistant.Tests.Inventory;

public sealed class AutoTrashPolicyTests
{
    [Fact]
    public void Decide_TrashesOnlyNewlyAcquiredQuantity()
    {
        AutoTrashDecision decision = AutoTrashPolicy.Decide(Conditions(
            acquiredQuantity: 2,
            currentStack: 12));

        Assert.Equal(new AutoTrashDecision(true, 2), decision);
    }

    [Fact]
    public void Decide_ClampsQuantityToCurrentStack()
    {
        AutoTrashDecision decision = AutoTrashPolicy.Decide(Conditions(
            acquiredQuantity: 20,
            currentStack: 4));

        Assert.Equal(new AutoTrashDecision(true, 4), decision);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void Decide_KeepsItemsWhenAutomationOptionOrTrashabilityDeniesIt(
        bool automationEnabled,
        bool autoTrashEnabled,
        bool canBeTrashed)
    {
        AutoTrashDecision decision = AutoTrashPolicy.Decide(Conditions(
            automationEnabled: automationEnabled,
            autoTrashEnabled: autoTrashEnabled,
            canBeTrashed: canBeTrashed));

        Assert.Equal(new AutoTrashDecision(false, 0), decision);
    }

    [Fact]
    public void Decide_KeepsItemsOutsideJunkList()
    {
        AutoTrashDecision decision = AutoTrashPolicy.Decide(Conditions(
            itemId: "(O)169",
            junkList: ["(O)168"]));

        Assert.False(decision.ShouldTrash);
    }

    [Fact]
    public void Decide_IgnoreListWinsEvenWhenItemIsAlsoJunk()
    {
        AutoTrashDecision decision = AutoTrashPolicy.Decide(Conditions(
            ignoreList: ["(o)168"]));

        Assert.False(decision.ShouldTrash);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Decide_RequiresExplicitPermissionForFish(bool allowTrashFish, bool expected)
    {
        AutoTrashDecision decision = AutoTrashPolicy.Decide(Conditions(
            isFish: true,
            allowTrashFish: allowTrashFish));

        Assert.Equal(expected, decision.ShouldTrash);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Decide_IgnoresNonPositiveAcquisition(int quantity)
    {
        Assert.False(AutoTrashPolicy.Decide(Conditions(acquiredQuantity: quantity)).ShouldTrash);
    }

    private static AutoTrashConditions Conditions(
        bool automationEnabled = true,
        bool autoTrashEnabled = true,
        string itemId = "(O)168",
        bool canBeTrashed = true,
        bool isFish = false,
        bool allowTrashFish = false,
        int acquiredQuantity = 1,
        int currentStack = 1,
        IReadOnlyCollection<string>? junkList = null,
        IReadOnlyCollection<string>? ignoreList = null)
    {
        return new AutoTrashConditions(
            automationEnabled,
            autoTrashEnabled,
            itemId,
            canBeTrashed,
            isFish,
            allowTrashFish,
            acquiredQuantity,
            currentStack,
            junkList ?? ["(O)168"],
            ignoreList ?? []);
    }
}
