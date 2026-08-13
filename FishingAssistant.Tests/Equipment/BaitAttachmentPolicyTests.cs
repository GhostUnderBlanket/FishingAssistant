using FishingAssistant.Equipment;

namespace FishingAssistant.Tests.Equipment;

public sealed class BaitAttachmentPolicyTests
{
    private static readonly BaitInventoryCandidate WildBait = new(2, "(O)774");
    private static readonly BaitInventoryCandidate BasicBait = new(4, "(O)685");

    private static BaitAttachmentConditions SafeConditions => new(
        true, true, true, null, 0, BaitAttachmentPolicy.AnyPreference, false,
        [WildBait, BasicBait]);

    [Fact]
    public void Decide_AnyPreferenceUsesFirstInventoryBait()
    {
        BaitAttachmentDecision result = BaitAttachmentPolicy.Decide(SafeConditions);

        Assert.Equal(BaitAttachmentAction.AttachFromInventory, result.Action);
        Assert.Equal(WildBait.InventoryIndex, result.InventoryIndex);
    }

    [Fact]
    public void Decide_SpecificPreferenceUsesMatchingBait()
    {
        BaitAttachmentConditions conditions = SafeConditions with { PreferredBaitId = BasicBait.QualifiedItemId };

        BaitAttachmentDecision result = BaitAttachmentPolicy.Decide(conditions);

        Assert.Equal(BaitAttachmentAction.AttachFromInventory, result.Action);
        Assert.Equal(BasicBait.InventoryIndex, result.InventoryIndex);
    }

    [Fact]
    public void Decide_RefillsOnlyTheCurrentlyAttachedBaitType()
    {
        BaitAttachmentConditions conditions = SafeConditions with
        {
            AttachedBaitId = BasicBait.QualifiedItemId,
            AttachedBaitSpace = 20,
            PreferredBaitId = WildBait.QualifiedItemId
        };

        BaitAttachmentDecision result = BaitAttachmentPolicy.Decide(conditions);

        Assert.Equal(BaitAttachmentAction.RefillFromInventory, result.Action);
        Assert.Equal(BasicBait.InventoryIndex, result.InventoryIndex);
    }

    [Fact]
    public void Decide_DoesNotReplaceFullAttachedBait()
    {
        BaitAttachmentConditions conditions = SafeConditions with
        {
            AttachedBaitId = BasicBait.QualifiedItemId,
            AttachedBaitSpace = 0
        };

        Assert.Equal(BaitAttachmentAction.None, BaitAttachmentPolicy.Decide(conditions).Action);
    }

    [Fact]
    public void Decide_SpawnsBasicBaitForAnyPreference()
    {
        BaitAttachmentConditions conditions = SafeConditions with
        {
            Candidates = [],
            SpawnIfMissing = true
        };

        BaitAttachmentDecision result = BaitAttachmentPolicy.Decide(conditions);

        Assert.Equal(BaitAttachmentAction.Spawn, result.Action);
        Assert.Equal(BaitAttachmentPolicy.DefaultSpawnBaitId, result.SpawnItemId);
    }

    [Fact]
    public void Decide_SpawnsTheSpecificPreferredBait()
    {
        BaitAttachmentConditions conditions = SafeConditions with
        {
            Candidates = [],
            PreferredBaitId = WildBait.QualifiedItemId,
            SpawnIfMissing = true
        };

        Assert.Equal(WildBait.QualifiedItemId, BaitAttachmentPolicy.Decide(conditions).SpawnItemId);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void Decide_DoesNothingWhenDisabledOrUnsafe(bool enabled, bool safe, bool supportsBait)
    {
        BaitAttachmentConditions conditions = SafeConditions with
        {
            AutoAttachEnabled = enabled,
            IsSafeToAttach = safe,
            RodSupportsBait = supportsBait
        };

        Assert.Equal(BaitAttachmentAction.None, BaitAttachmentPolicy.Decide(conditions).Action);
    }
}
