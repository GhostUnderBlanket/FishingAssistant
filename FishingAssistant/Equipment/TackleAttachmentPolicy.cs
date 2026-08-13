namespace FishingAssistant.Equipment;

internal enum TackleAttachmentAction
{
    None,
    AttachFromInventory,
    Spawn
}

internal sealed record TackleInventoryCandidate(int InventoryIndex, string QualifiedItemId);

internal sealed record TackleSlotState(int SlotIndex, bool IsOccupied, string PreferredTackleId);

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
    public const string AnyPreference = "Any";
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
            bool useAny = IsAny(target.PreferredTackleId);
            TackleInventoryCandidate? selected = conditions.Candidates.FirstOrDefault(candidate =>
                useAny || string.Equals(candidate.QualifiedItemId, target.PreferredTackleId,
                    StringComparison.OrdinalIgnoreCase));
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
        string spawnItemId = IsAny(spawnTarget.PreferredTackleId)
            ? DefaultSpawnTackleId
            : spawnTarget.PreferredTackleId;
        return new TackleAttachmentDecision(
            TackleAttachmentAction.Spawn,
            spawnTarget.SlotIndex,
            SpawnItemId: spawnItemId);
    }

    private static bool IsAny(string preference)
    {
        return string.Equals(preference, AnyPreference, StringComparison.OrdinalIgnoreCase);
    }
}
