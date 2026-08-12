using FishingAssistant.Configuration;

namespace FishingAssistant.Tests.Configuration;

public sealed class ConfigDefaultsTests
{
    [Fact]
    public void Defaults_AreSafeAndMatchLegacyCoreBehavior()
    {
        ModConfig config = new();

        Assert.Equal(ModConfig.CurrentVersion, config.ConfigVersion);
        Assert.Equal("F5", config.EnableAutomationButton.ToString());
        Assert.Equal("F6", config.CatchTreasureButton.ToString());
        Assert.Equal("None", config.OpenConfigMenuButton.ToString());
        Assert.True(config.AutoCastFishingRod);
        Assert.True(config.AutoHookFish);
        Assert.True(config.AutoPlayMiniGame);
        Assert.True(config.AutoClosePopup);
        Assert.True(config.AutoLootTreasure);
        Assert.False(config.AutoTrashJunk);
        Assert.False(config.AutoEatFood);
        Assert.False(config.SpawnBaitIfDontHave);
        Assert.False(config.SpawnTackleIfDontHave);
        Assert.Equal(SkipMinigameBehavior.Off, config.SkipFishingMiniGame);
        Assert.Equal(TreasureChanceBehavior.Default, config.TreasureChance);
        Assert.Equal(TreasureChanceBehavior.Default, config.GoldenTreasureChance);
    }

    [Fact]
    public void CreateDraft_DeepCopiesMutableValues()
    {
        ModConfig active = new()
        {
            JunkIgnoreList = ["(O)168"]
        };

        ModConfig draft = active.CreateDraft();
        draft.JunkIgnoreList.Add("(O)169");

        Assert.NotSame(active.JunkIgnoreList, draft.JunkIgnoreList);
        Assert.Single(active.JunkIgnoreList);
        Assert.Equal(active.EnableAutomationButton.ToString(), draft.EnableAutomationButton.ToString());
        Assert.NotSame(active.EnableAutomationButton, draft.EnableAutomationButton);
    }
}
