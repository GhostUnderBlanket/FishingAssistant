namespace FishingAssistant.Configuration;

internal enum ConfigItemKind
{
    Other,
    Bait,
    Tackle,
    FishingRod
}

internal sealed record ConfigItem(string QualifiedItemId, ConfigItemKind Kind);

internal interface IItemCatalog
{
    ConfigItem? Find(string itemId);
}
