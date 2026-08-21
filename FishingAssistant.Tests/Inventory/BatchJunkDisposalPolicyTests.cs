using FishingAssistant.Configuration;
using FishingAssistant.Inventory;

namespace FishingAssistant.Tests.Inventory;

public sealed class BatchJunkDisposalPolicyTests
{
    [Fact]
    public void Select_ReturnsEveryEligibleJunkStackInInventoryOrder()
    {
        IReadOnlyList<int> selected = BatchJunkDisposalPolicy.Select(Conditions(candidates:
        [
            new(2, "(O)168", true, false, 4),
            new(5, "(O)390", true, false, 1),
            new(7, "(O)169", true, false, 2)
        ]));

        Assert.Equal([2, 7], selected);
    }

    [Theory]
    [InlineData(false, (int)JunkDisposalMode.WhenInventoryFull, true)]
    [InlineData(true, (int)JunkDisposalMode.Off, true)]
    [InlineData(true, (int)JunkDisposalMode.Immediately, true)]
    [InlineData(true, (int)JunkDisposalMode.WhenInventoryFull, false)]
    public void Select_RequiresAutomationFullInventoryAndBatchMode(
        bool automationEnabled,
        int modeValue,
        bool isInventoryFull)
    {
        IReadOnlyList<int> selected = BatchJunkDisposalPolicy.Select(Conditions(
            automationEnabled: automationEnabled,
            mode: (JunkDisposalMode)modeValue,
            isInventoryFull: isInventoryFull));

        Assert.Empty(selected);
    }

    [Fact]
    public void Select_SkipsNonTrashableAndEmptyCandidates()
    {
        IReadOnlyList<int> selected = BatchJunkDisposalPolicy.Select(Conditions(candidates:
        [
            new(0, "(O)168", false, false, 1),
            new(1, "(O)168", true, false, 0),
            new(2, "(O)169", true, false, 2)
        ]));

        Assert.Equal([2], selected);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Select_RequiresExplicitPermissionForFish(bool allowTrashFish, bool expected)
    {
        IReadOnlyList<int> selected = BatchJunkDisposalPolicy.Select(Conditions(
            allowTrashFish: allowTrashFish,
            candidates: [new(0, "(O)168", true, true, 1)]));

        Assert.Equal(expected, selected.Count == 1);
    }

    private static BatchJunkDisposalConditions Conditions(
        bool automationEnabled = true,
        JunkDisposalMode mode = JunkDisposalMode.WhenInventoryFull,
        bool isInventoryFull = true,
        bool allowTrashFish = false,
        IReadOnlyCollection<string>? junkList = null,
        IReadOnlyList<BatchJunkCandidate>? candidates = null)
    {
        return new BatchJunkDisposalConditions(
            automationEnabled,
            mode,
            isInventoryFull,
            allowTrashFish,
            junkList ?? ["(O)168", "(O)169"],
            candidates ?? [new(0, "(O)168", true, false, 1)]);
    }
}
