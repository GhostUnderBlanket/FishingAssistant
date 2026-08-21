namespace FishingAssistant.Equipment;

internal enum TackleAttachmentAction
{
    None,
    AttachFromInventory,
    Spawn
}

internal sealed record TackleInventoryCandidate(int InventoryIndex, string QualifiedItemId);

internal sealed record TackleSlotState(
    int SlotIndex,
    bool IsOccupied,
    IReadOnlyList<string> PreferredTackleIds);

internal sealed record TackleAttachmentConditions(
    bool AutoAttachEnabled,
    bool IsSafeToAttach,
    bool RodSupportsTackle,
    bool SpawnIfMissing,
    IReadOnlyList<TackleSlotState> Slots,
    IReadOnlyList<TackleInventoryCandidate> Candidates);

internal sealed record TackleAttachmentDecision(
    TackleAttachmentAction Action,
    int TargetSlot = -1,
    int InventoryIndex = -1,
    string? SpawnItemId = null);

internal static class TackleAttachmentPolicy
{
    public const string DefaultSpawnTackleId = "(O)686";

    public static TackleAttachmentDecision Decide(TackleAttachmentConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (!conditions.AutoAttachEnabled || !conditions.IsSafeToAttach || !conditions.RodSupportsTackle)
            return new TackleAttachmentDecision(TackleAttachmentAction.None);

        TackleSlotState[] emptySlots = conditions.Slots
            .OrderBy(slot => slot.SlotIndex)
            .Where(slot => !slot.IsOccupied)
            .ToArray();
        if (emptySlots.Length == 0)
            return new TackleAttachmentDecision(TackleAttachmentAction.None);

        foreach (TackleSlotState target in emptySlots)
        {
            string? firstPreference = target.PreferredTackleIds.FirstOrDefault();
            TackleInventoryCandidate? selected = conditions.SpawnIfMissing && firstPreference is not null
                ? conditions.Candidates.FirstOrDefault(candidate =>
                    string.Equals(candidate.QualifiedItemId, firstPreference, StringComparison.OrdinalIgnoreCase))
                : SelectFirstAvailable(target.PreferredTackleIds, conditions.Candidates);
            if (selected is not null)
            {
                return new TackleAttachmentDecision(
                    TackleAttachmentAction.AttachFromInventory,
                    target.SlotIndex,
                    selected.InventoryIndex);
            }
        }

        if (!conditions.SpawnIfMissing)
            return new TackleAttachmentDecision(TackleAttachmentAction.None);

        TackleSlotState spawnTarget = emptySlots[0];
        string spawnItemId = spawnTarget.PreferredTackleIds.FirstOrDefault() ?? DefaultSpawnTackleId;
        return new TackleAttachmentDecision(
            TackleAttachmentAction.Spawn,
            spawnTarget.SlotIndex,
            SpawnItemId: spawnItemId);
    }

    private static TackleInventoryCandidate? SelectFirstAvailable(
        IReadOnlyList<string> preferences,
        IReadOnlyList<TackleInventoryCandidate> candidates)
    {
        if (preferences.Count == 0)
            return candidates.FirstOrDefault();

        foreach (string preference in preferences)
        {
            TackleInventoryCandidate? candidate = candidates.FirstOrDefault(item =>
                string.Equals(item.QualifiedItemId, preference, StringComparison.OrdinalIgnoreCase));
            if (candidate is not null)
                return candidate;
        }

        return null;
    }
}
