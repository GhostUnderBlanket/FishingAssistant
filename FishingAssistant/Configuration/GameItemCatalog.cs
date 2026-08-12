using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace FishingAssistant.Configuration;

internal sealed class GameItemCatalog : IItemCatalog
{
    private static readonly HashSet<string> SupportedStarterRods =
    [
        "(T)TrainingRod",
        "(T)BambooPole"
    ];

    public ConfigItem? Find(string itemId)
    {
        ParsedItemData? data = ItemRegistry.GetData(itemId);
        if (data is null)
            return null;

        ConfigItemKind kind = data.Category switch
        {
            StardewValley.Object.baitCategory => ConfigItemKind.Bait,
            StardewValley.Object.tackleCategory => ConfigItemKind.Tackle,
            _ when SupportedStarterRods.Contains(data.QualifiedItemId) => ConfigItemKind.FishingRod,
            _ => ConfigItemKind.Other
        };

        return new ConfigItem(data.QualifiedItemId, kind);
    }
}
