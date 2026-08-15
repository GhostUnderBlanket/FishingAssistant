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
        Assert.Equal("F6", config.OpenConfigMenuButton.ToString());
        Assert.False(config.ToggleTreasureTargetingButton.IsBound);
        Assert.Equal(AutomationProfile.Relaxed, config.AutomationProfile);
        Assert.True(config.AutoCastFishingRod);
        Assert.True(config.AutoHookFish);
        Assert.True(config.AutoPlayMiniGame);
        Assert.True(config.AutoClosePopup);
        Assert.True(config.AutoLootTreasure);
        Assert.Equal(InventoryFullAction.Stop, config.ActionIfInventoryFull);
        Assert.Empty(config.TreasureChestIgnoreList);
        Assert.Equal(IgnoredTreasureAction.KeepOpen, config.ActionIfOnlyIgnoredTreasureRemains);
        Assert.False(config.AutoTrashJunk);
        Assert.False(config.AllowTrashFish);
        Assert.Equal(["(O)168", "(O)169", "(O)170", "(O)171", "(O)172"], config.JunkList);
        Assert.False(config.AutoEatFood);
        Assert.False(config.AllowEatingFish);
        Assert.False(config.AutoAttachBait);
        Assert.False(config.SpawnBaitIfDontHave);
        Assert.False(config.AutoAttachTackles);
        Assert.False(config.SpawnTackleIfDontHave);
        Assert.False(config.InfiniteBait);
        Assert.False(config.InfiniteTackle);
        Assert.Equal(SkipMinigameBehavior.Off, config.SkipFishingMiniGame);
        Assert.True(config.AutomaticBubbleSteering);
        Assert.Equal(TreasureChanceBehavior.Default, config.TreasureChance);
        Assert.Equal(TreasureChanceBehavior.Default, config.GoldenTreasureChance);
        Assert.False(config.TreasureTargeting);
        Assert.Equal(FishPreviewStyle.Sonar, config.FishPreviewStyle);
        Assert.Equal("None", config.StartWithFishingRod);
        Assert.Equal(1f, config.AutoCastDelaySeconds);
    }

    [Fact]
    public void CreateDraft_DeepCopiesMutableValues()
    {
        ModConfig active = new()
        {
            JunkList = ["(O)167"],
            TreasureChestIgnoreList = ["(O)169"]
        };

        ModConfig draft = active.CreateDraft();
        draft.JunkList.Add("(O)170");
        draft.TreasureChestIgnoreList.Add("(O)170");

        Assert.NotSame(active.JunkList, draft.JunkList);
        Assert.Single(active.JunkList);
        Assert.NotSame(active.TreasureChestIgnoreList, draft.TreasureChestIgnoreList);
        Assert.Single(active.TreasureChestIgnoreList);
        Assert.Equal(active.EnableAutomationButton.ToString(), draft.EnableAutomationButton.ToString());
        Assert.NotSame(active.EnableAutomationButton, draft.EnableAutomationButton);
        Assert.Equal(active.ToggleTreasureTargetingButton.ToString(), draft.ToggleTreasureTargetingButton.ToString());
        Assert.NotSame(active.ToggleTreasureTargetingButton, draft.ToggleTreasureTargetingButton);
    }
}
