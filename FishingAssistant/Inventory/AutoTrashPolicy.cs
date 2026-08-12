namespace FishingAssistant.Inventory;

internal sealed record AutoTrashConditions(
    bool AutomationEnabled,
    bool AutoTrashEnabled,
    string QualifiedItemId,
    bool CanBeTrashed,
    bool IsFish,
    bool AllowTrashFish,
    int AcquiredQuantity,
    int CurrentStack,
    IReadOnlyCollection<string> JunkList,
    IReadOnlyCollection<string> IgnoreList);

internal sealed record AutoTrashDecision(bool ShouldTrash, int Quantity);

internal static class AutoTrashPolicy
{
    public static AutoTrashDecision Decide(AutoTrashConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        bool isIgnored = conditions.IgnoreList.Contains(
            conditions.QualifiedItemId,
            StringComparer.OrdinalIgnoreCase);
        bool isJunk = conditions.JunkList.Contains(
            conditions.QualifiedItemId,
            StringComparer.OrdinalIgnoreCase);
        bool isEligible = conditions.AutomationEnabled
            && conditions.AutoTrashEnabled
            && conditions.CanBeTrashed
            && isJunk
            && !isIgnored
            && (!conditions.IsFish || conditions.AllowTrashFish);
        if (!isEligible)
            return new(false, 0);

        int quantity = Math.Clamp(conditions.AcquiredQuantity, 0, Math.Max(0, conditions.CurrentStack));
        return new(quantity > 0, quantity);
    }
}
