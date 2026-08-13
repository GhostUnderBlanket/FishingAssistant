namespace FishingAssistant.Inventory;

internal enum AutoEatAction
{
    None,
    Eat
}

internal sealed record FoodInventoryCandidate(
    int InventoryIndex,
    string QualifiedItemId,
    int StaminaRecovery,
    int SalePrice,
    bool IsFish,
    bool IsQuestOrProgressionItem,
    bool HasBuff,
    bool IsBlockedByFullness);

internal sealed record AutoEatConditions(
    bool AutoEatEnabled,
    bool AutomationEnabled,
    bool IsSafeToEat,
    float Stamina,
    float MaxStamina,
    int EnergyThresholdPercent,
    bool AllowEatingFish,
    IReadOnlyList<FoodInventoryCandidate> Candidates);

internal sealed record AutoEatDecision(AutoEatAction Action, int InventoryIndex = -1);

internal static class AutoEatPolicy
{
    public static AutoEatDecision Decide(AutoEatConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (!conditions.AutoEatEnabled
            || !conditions.AutomationEnabled
            || !conditions.IsSafeToEat
            || conditions.MaxStamina <= 0f
            || conditions.Stamina > conditions.MaxStamina * conditions.EnergyThresholdPercent / 100f)
        {
            return new AutoEatDecision(AutoEatAction.None);
        }

        int missingStamina = Math.Max(1, (int)Math.Ceiling(conditions.MaxStamina - conditions.Stamina));
        List<FoodInventoryCandidate> eligible = conditions.Candidates
            .Where(candidate => candidate.InventoryIndex >= 0
                && candidate.StaminaRecovery > 0
                && !candidate.IsQuestOrProgressionItem
                && !candidate.IsBlockedByFullness
                && (conditions.AllowEatingFish || !candidate.IsFish))
            .ToList();
        if (eligible.Count == 0)
            return new AutoEatDecision(AutoEatAction.None);

        // Avoid spending a buff food for energy when a plain food is available.
        IReadOnlyList<FoodInventoryCandidate> pool = eligible.Any(candidate => !candidate.HasBuff)
            ? eligible.Where(candidate => !candidate.HasBuff).ToList()
            : eligible;
        FoodInventoryCandidate selected = pool
            .OrderBy(candidate => GetCostPerUsefulEnergy(candidate, missingStamina))
            .ThenBy(candidate => Math.Max(0, candidate.StaminaRecovery - missingStamina))
            .ThenBy(candidate => Math.Max(0, candidate.SalePrice))
            .ThenBy(candidate => candidate.InventoryIndex)
            .First();

        return new AutoEatDecision(AutoEatAction.Eat, selected.InventoryIndex);
    }

    private static decimal GetCostPerUsefulEnergy(FoodInventoryCandidate candidate, int missingStamina)
    {
        int usefulEnergy = Math.Max(1, Math.Min(candidate.StaminaRecovery, missingStamina));
        return Math.Max(0, candidate.SalePrice) / (decimal)usefulEnergy;
    }
}
