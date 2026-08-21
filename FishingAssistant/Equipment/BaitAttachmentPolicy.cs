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
    IReadOnlyList<string> PreferredBaitIds,
    bool SpawnIfMissing,
    IReadOnlyList<BaitInventoryCandidate> Candidates);

internal sealed record BaitAttachmentDecision(
    BaitAttachmentAction Action,
    int InventoryIndex = -1,
    string? SpawnItemId = null);

internal static class BaitAttachmentPolicy
{
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

        bool useAny = conditions.PreferredBaitIds.Count == 0;
        string? preferredId = useAny ? null : conditions.PreferredBaitIds[0];
        BaitInventoryCandidate? selected = conditions.SpawnIfMissing && preferredId is not null
            ? conditions.Candidates.FirstOrDefault(candidate =>
                string.Equals(candidate.QualifiedItemId, preferredId, StringComparison.OrdinalIgnoreCase))
            : SelectFirstAvailable(conditions.PreferredBaitIds, conditions.Candidates);
        if (selected is not null)
            return new BaitAttachmentDecision(BaitAttachmentAction.AttachFromInventory, selected.InventoryIndex);

        if (!conditions.SpawnIfMissing)
            return new BaitAttachmentDecision(BaitAttachmentAction.None);

        string spawnItemId = useAny ? DefaultSpawnBaitId : preferredId!;
        return new BaitAttachmentDecision(BaitAttachmentAction.Spawn, SpawnItemId: spawnItemId);
    }

    private static BaitInventoryCandidate? SelectFirstAvailable(
        IReadOnlyList<string> preferences,
        IReadOnlyList<BaitInventoryCandidate> candidates)
    {
        if (preferences.Count == 0)
            return candidates.FirstOrDefault();

        foreach (string preference in preferences)
        {
            BaitInventoryCandidate? candidate = candidates.FirstOrDefault(item =>
                string.Equals(item.QualifiedItemId, preference, StringComparison.OrdinalIgnoreCase));
            if (candidate is not null)
                return candidate;
        }

        return null;
    }
}
