namespace FishingAssistant.Equipment;

internal enum BaitAttachmentAction
{
    None,
    AttachFromInventory,
    RefillFromInventory,
    Spawn
}

internal sealed record BaitInventoryCandidate(int InventoryIndex, string QualifiedItemId);

internal sealed record BaitAttachmentConditions(
    bool AutoAttachEnabled,
    bool IsSafeToAttach,
    bool RodSupportsBait,
    string? AttachedBaitId,
    int AttachedBaitSpace,
    string PreferredBaitId,
    bool SpawnIfMissing,
    IReadOnlyList<BaitInventoryCandidate> Candidates);

internal sealed record BaitAttachmentDecision(
    BaitAttachmentAction Action,
    int InventoryIndex = -1,
    string? SpawnItemId = null);

internal static class BaitAttachmentPolicy
{
    public const string AnyPreference = "Any";
    public const string DefaultSpawnBaitId = "(O)685";

    public static BaitAttachmentDecision Decide(BaitAttachmentConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (!conditions.AutoAttachEnabled || !conditions.IsSafeToAttach || !conditions.RodSupportsBait)
            return new BaitAttachmentDecision(BaitAttachmentAction.None);

        if (conditions.AttachedBaitId is not null)
        {
            if (conditions.AttachedBaitSpace <= 0)
                return new BaitAttachmentDecision(BaitAttachmentAction.None);

            BaitInventoryCandidate? refill = conditions.Candidates.FirstOrDefault(candidate =>
                string.Equals(candidate.QualifiedItemId, conditions.AttachedBaitId,
                    StringComparison.OrdinalIgnoreCase));
            return refill is null
                ? new BaitAttachmentDecision(BaitAttachmentAction.None)
                : new BaitAttachmentDecision(BaitAttachmentAction.RefillFromInventory, refill.InventoryIndex);
        }

        bool useAny = string.Equals(conditions.PreferredBaitId, AnyPreference,
            StringComparison.OrdinalIgnoreCase);
        BaitInventoryCandidate? selected = conditions.Candidates.FirstOrDefault(candidate =>
            useAny || string.Equals(candidate.QualifiedItemId, conditions.PreferredBaitId,
                StringComparison.OrdinalIgnoreCase));
        if (selected is not null)
            return new BaitAttachmentDecision(BaitAttachmentAction.AttachFromInventory, selected.InventoryIndex);

        if (!conditions.SpawnIfMissing)
            return new BaitAttachmentDecision(BaitAttachmentAction.None);

        string spawnItemId = useAny ? DefaultSpawnBaitId : conditions.PreferredBaitId;
        return new BaitAttachmentDecision(BaitAttachmentAction.Spawn, SpawnItemId: spawnItemId);
    }
}
