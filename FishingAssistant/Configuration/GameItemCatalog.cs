using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace FishingAssistant.Configuration;

internal sealed class GameItemCatalog : IItemCatalog, IConfigItemSource
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

        return new ConfigItem(data.QualifiedItemId, kind, data.DisplayName);
    }

    public IReadOnlyList<ConfigItem> GetAll(ConfigItemKind kind)
    {
        IEnumerable<string> itemIds = kind switch
        {
            ConfigItemKind.Bait => Game1.objectData
                .Where(pair => pair.Value.Category == StardewValley.Object.baitCategory)
                .Select(pair => ItemRegistry.ManuallyQualifyItemId(pair.Key, "(O)")),
            ConfigItemKind.Tackle => Game1.objectData
                .Where(pair => pair.Value.Category == StardewValley.Object.tackleCategory)
                .Select(pair => ItemRegistry.ManuallyQualifyItemId(pair.Key, "(O)")),
            ConfigItemKind.FishingRod => SupportedStarterRods,
            _ => []
        };

        return itemIds
            .Select(this.Find)
            .Where(item => item?.Kind == kind)
            .Select(item => item!)
            .DistinctBy(item => item.QualifiedItemId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.QualifiedItemId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
