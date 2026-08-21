using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class ConfigCategoryTests
{
    [Fact]
    public void Categories_FollowConfiguredMenuOrder()
    {
        Assert.Equal(
            [
                ConfigCategory.Automation,
                ConfigCategory.Fishing,
                ConfigCategory.Minigame,
                ConfigCategory.Display,
                ConfigCategory.Inventory,
                ConfigCategory.Equipment,
                ConfigCategory.Enchantments,
                ConfigCategory.Controls,
                ConfigCategory.Debug
            ],
            Enum.GetValues<ConfigCategory>());
    }
}
