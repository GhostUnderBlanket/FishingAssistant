using FishingAssistant.Configuration;

namespace FishingAssistant.Inventory;

internal sealed record BatchJunkCandidate(
    int InventoryIndex,
    string QualifiedItemId,
    bool CanBeTrashed,
    int Quantity);

internal sealed record BatchJunkDisposalConditions(
    bool AutomationEnabled,
    bool HasFishingRod,
    JunkDisposalMode Mode,
    bool IsInventoryFull,
    IReadOnlyCollection<string> JunkList,
    IReadOnlyList<BatchJunkCandidate> Candidates);

internal static class BatchJunkDisposalPolicy
{
    public static IReadOnlyList<int> Select(BatchJunkDisposalConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (!conditions.AutomationEnabled
            || !conditions.HasFishingRod
            || conditions.Mode != JunkDisposalMode.WhenInventoryFull
            || !conditions.IsInventoryFull)
        {
            return [];
        }

        List<int> selected = [];
        foreach (BatchJunkCandidate candidate in conditions.Candidates)
        {
            bool isJunk = conditions.JunkList.Contains(
                candidate.QualifiedItemId,
                StringComparer.OrdinalIgnoreCase);
            if (candidate.InventoryIndex >= 0
                && candidate.Quantity > 0
                && candidate.CanBeTrashed
                && isJunk)
            {
                selected.Add(candidate.InventoryIndex);
            }
        }

        return selected;
    }
}
