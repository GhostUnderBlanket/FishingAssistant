namespace FishingAssistant.Equipment;

internal enum RodEnchantmentKind
{
    AutoHook,
    Efficient,
    Master,
    Preserving
}

internal sealed record RodEnchantmentConditions(
    bool HasRemotePlayers,
    bool IsEquipped,
    bool RemoveWhenUnequipped,
    IReadOnlySet<RodEnchantmentKind> Requested,
    IReadOnlySet<RodEnchantmentKind> Existing,
    IReadOnlySet<RodEnchantmentKind> Managed);

internal sealed record RodEnchantmentDecision(
    IReadOnlyList<RodEnchantmentKind> Add,
    IReadOnlyList<RodEnchantmentKind> Remove,
    bool IsUnsupportedMultiplayer);

internal static class RodEnchantmentPolicy
{
    public static RodEnchantmentDecision Decide(RodEnchantmentConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        List<RodEnchantmentKind> remove = conditions.Managed
            .Where(kind => conditions.HasRemotePlayers
                || !conditions.Requested.Contains(kind)
                || !conditions.IsEquipped && conditions.RemoveWhenUnequipped)
            .OrderBy(kind => kind)
            .ToList();
        HashSet<RodEnchantmentKind> remaining = [.. conditions.Existing];
        foreach (RodEnchantmentKind kind in remove)
            remaining.Remove(kind);

        List<RodEnchantmentKind> add = conditions.HasRemotePlayers || !conditions.IsEquipped
            ? []
            : conditions.Requested
                .Where(kind => !remaining.Contains(kind))
                .OrderBy(kind => kind)
                .ToList();
        return new RodEnchantmentDecision(
            add,
            remove,
            conditions.HasRemotePlayers && conditions.Requested.Count > 0);
    }
}
