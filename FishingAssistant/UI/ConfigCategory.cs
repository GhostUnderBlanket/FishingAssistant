namespace FishingAssistant.UI;

internal enum ConfigCategory
{
    Automation,
    Fishing,
    Minigame,
    Treasure,
    Inventory,
    Equipment,
    QuickControls,
    Interface,
    Enchantments,
#if FISHING_ASSISTANT_TEST_BUILD
    Debug
#endif
}
