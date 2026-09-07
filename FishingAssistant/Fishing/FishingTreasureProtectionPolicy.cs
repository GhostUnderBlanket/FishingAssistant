using StardewValley;

namespace FishingAssistant.Fishing;

internal static class FishingTreasureProtectionPolicy
{
    private static readonly HashSet<string> EssentialFishingItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "(O)TroutDerbyTag",
        "(O)GoldenBobber"
    };

    public static bool IsSafetyCritical(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            if (EssentialFishingItemIds.Contains(item.QualifiedItemId)
                || !item.canBeTrashed()
                || !string.IsNullOrWhiteSpace(item.SetFlagOnPickup))
            {
                return true;
            }

            return item is StardewValley.Object { questItem.Value: true };
        }
        catch
        {
            // An unfamiliar modded item should fail safe. Keeping one extra item in
            // the menu is preferable to silently destroying a catch or quest item.
            return true;
        }
    }
}
