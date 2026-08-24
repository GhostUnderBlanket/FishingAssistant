namespace FishingAssistant.UI;

internal enum ConfigCategory
{
    Automation,
    Fishing,
    Minigame,
    Display,
    Inventory,
    Equipment,
    Enchantments,
    Controls,
#if FISHING_ASSISTANT_TEST_BUILD
    Debug
#endif
}
