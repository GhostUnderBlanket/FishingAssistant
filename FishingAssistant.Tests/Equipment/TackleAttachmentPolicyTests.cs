using FishingAssistant.Equipment;

namespace FishingAssistant.Tests.Equipment;

public sealed class TackleAttachmentPolicyTests
{
    private static readonly TackleInventoryCandidate Spinner = new(3, "(O)686");
    private static readonly TackleInventoryCandidate TrapBobber = new(7, "(O)694");

    private static TackleAttachmentConditions SafeConditions => new(
        true,
        true,
        true,
        false,
        [new TackleSlotState(1, false, [])],
        [TrapBobber, Spinner]);

    [Fact]
    public void Decide_AnyPreferenceUsesFirstInventoryTackle()
    {
        TackleAttachmentDecision result = TackleAttachmentPolicy.Decide(SafeConditions);

        Assert.Equal(TackleAttachmentAction.AttachFromInventory, result.Action);
        Assert.Equal(1, result.TargetSlot);
        Assert.Equal(TrapBobber.InventoryIndex, result.InventoryIndex);
    }

    [Fact]
    public void Decide_SpecificPreferenceUsesMatchingTackle()
    {
        TackleAttachmentConditions conditions = SafeConditions with
        {
            Slots = [new TackleSlotState(1, false, [Spinner.QualifiedItemId])]
        };

        TackleAttachmentDecision result = TackleAttachmentPolicy.Decide(conditions);

        Assert.Equal(Spinner.InventoryIndex, result.InventoryIndex);
    }

    [Fact]
    public void Decide_UsesSecondSlotPreferenceAfterFirstSlotIsOccupied()
    {
        TackleAttachmentConditions conditions = SafeConditions with
        {
            Slots =
            [
                new TackleSlotState(1, true, [TrapBobber.QualifiedItemId]),
                new TackleSlotState(2, false, [Spinner.QualifiedItemId])
            ]
        };

        TackleAttachmentDecision result = TackleAttachmentPolicy.Decide(conditions);

        Assert.Equal(2, result.TargetSlot);
        Assert.Equal(Spinner.InventoryIndex, result.InventoryIndex);
    }

    [Fact]
    public void Decide_CanFillSecondPreferenceWhenFirstPreferenceIsUnavailable()
    {
        TackleAttachmentConditions conditions = SafeConditions with
        {
            Slots =
            [
                new TackleSlotState(1, false, ["(O)MissingTackle"]),
                new TackleSlotState(2, false, [Spinner.QualifiedItemId])
            ]
        };

        TackleAttachmentDecision result = TackleAttachmentPolicy.Decide(conditions);

        Assert.Equal(TackleAttachmentAction.AttachFromInventory, result.Action);
        Assert.Equal(2, result.TargetSlot);
        Assert.Equal(Spinner.InventoryIndex, result.InventoryIndex);
    }

    [Fact]
    public void Decide_DoesNotReplaceOccupiedSlots()
    {
        TackleAttachmentConditions conditions = SafeConditions with
        {
            Slots = [new TackleSlotState(1, true, [])]
        };

        Assert.Equal(TackleAttachmentAction.None, TackleAttachmentPolicy.Decide(conditions).Action);
    }

    [Fact]
    public void Decide_SpawnsSpinnerForAnyPreference()
    {
        TackleAttachmentConditions conditions = SafeConditions with
        {
            Candidates = [],
            SpawnIfMissing = true
        };

        TackleAttachmentDecision result = TackleAttachmentPolicy.Decide(conditions);

        Assert.Equal(TackleAttachmentAction.Spawn, result.Action);
        Assert.Equal(TackleAttachmentPolicy.DefaultSpawnTackleId, result.SpawnItemId);
    }

    [Fact]
    public void Decide_SpawnsSpecificPreferredTackle()
    {
        TackleAttachmentConditions conditions = SafeConditions with
        {
            Slots = [new TackleSlotState(1, false, [TrapBobber.QualifiedItemId])],
            Candidates = [],
            SpawnIfMissing = true
        };

        Assert.Equal(TrapBobber.QualifiedItemId, TackleAttachmentPolicy.Decide(conditions).SpawnItemId);
    }

    [Fact]
    public void Decide_UsesFirstAvailableTackleInPreferenceOrder()
    {
        TackleAttachmentConditions conditions = SafeConditions with
        {
            Slots = [new TackleSlotState(1, false,
                ["(O)Missing", Spinner.QualifiedItemId, TrapBobber.QualifiedItemId])]
        };

        TackleAttachmentDecision result = TackleAttachmentPolicy.Decide(conditions);

        Assert.Equal(Spinner.InventoryIndex, result.InventoryIndex);
    }

    [Fact]
    public void Decide_SpawningPreservesFirstPreferencePriority()
    {
        TackleAttachmentConditions conditions = SafeConditions with
        {
            Slots = [new TackleSlotState(1, false, ["(O)Missing", TrapBobber.QualifiedItemId])],
            SpawnIfMissing = true
        };

        TackleAttachmentDecision result = TackleAttachmentPolicy.Decide(conditions);

        Assert.Equal(TackleAttachmentAction.Spawn, result.Action);
        Assert.Equal("(O)Missing", result.SpawnItemId);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void Decide_DoesNothingWhenDisabledOrUnsafe(bool enabled, bool safe, bool supportsTackle)
    {
        TackleAttachmentConditions conditions = SafeConditions with
        {
            AutoAttachEnabled = enabled,
            IsSafeToAttach = safe,
            RodSupportsTackle = supportsTackle
        };

        Assert.Equal(TackleAttachmentAction.None, TackleAttachmentPolicy.Decide(conditions).Action);
    }
}
