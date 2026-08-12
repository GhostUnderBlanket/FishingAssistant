namespace FishingAssistant.Configuration;

internal enum ConfigItemKind
{
    Other,
    Bait,
    Tackle,
    FishingRod
}

internal sealed record ConfigItem(string QualifiedItemId, ConfigItemKind Kind, string DisplayName = "");

internal interface IItemCatalog
{
    ConfigItem? Find(string itemId);
}

internal interface IConfigItemSource
{
    IReadOnlyList<ConfigItem> GetAll(ConfigItemKind kind);
}
