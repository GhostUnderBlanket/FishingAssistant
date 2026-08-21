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
        Assert.False(config.IgnoreJunkListItemsInTreasureChests);
        Assert.Equal(IgnoredTreasureAction.KeepOpen, config.ActionIfOnlyIgnoredTreasureRemains);
        Assert.Equal(JunkDisposalMode.WhenInventoryFull, config.JunkDisposalMode);
        Assert.False(config.AutoTrashJunk);
        Assert.False(config.AllowTrashFish);
        Assert.Equal(["(O)168", "(O)169", "(O)170", "(O)171", "(O)172"], config.JunkList);
        Assert.False(config.AutoEatFood);
        Assert.False(config.AllowEatingFish);
        Assert.False(config.AutoAttachBait);
        Assert.Empty(config.PreferredBaits);
        Assert.False(config.SpawnBaitIfDontHave);
        Assert.False(config.AutoAttachTackles);
        Assert.Empty(config.PreferredTackles);
        Assert.Empty(config.PreferredSecondTackles);
        Assert.False(config.SpawnTackleIfDontHave);
        Assert.False(config.InfiniteBait);
        Assert.False(config.InfiniteTackle);
        Assert.Equal(SkipMinigameBehavior.Off, config.SkipFishingMiniGame);
        Assert.Equal(MinigameAssistancePreset.Off, config.MinigameAssistance);
        Assert.Equal(100, config.FishSpeedPercent);
        Assert.Equal(100, config.ProgressGainPercent);
        Assert.Equal(100, config.ProgressLossPercent);
        Assert.Equal(100, config.TreasureSpeedPercent);
        Assert.Equal(100, config.BarSizePercent);
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
            MinigameAssistance = MinigameAssistancePreset.Comfortable,
            FishSpeedPercent = 70,
            ProgressGainPercent = 135,
            ProgressLossPercent = 60,
            TreasureSpeedPercent = 160,
            BarSizePercent = 125,
            JunkList = ["(O)167"],
            TreasureChestIgnoreList = ["(O)169"],
            PreferredBaits = ["(O)685"],
            PreferredTackles = ["(O)686"],
            PreferredSecondTackles = ["(O)687"]
        };

        ModConfig draft = active.CreateDraft();
        draft.JunkList.Add("(O)170");
        draft.TreasureChestIgnoreList.Add("(O)170");
        draft.PreferredBaits.Add("(O)774");
        draft.PreferredTackles.Clear();
        draft.PreferredSecondTackles.Add("(O)694");

        Assert.NotSame(active.JunkList, draft.JunkList);
        Assert.Single(active.JunkList);
        Assert.NotSame(active.TreasureChestIgnoreList, draft.TreasureChestIgnoreList);
        Assert.Single(active.TreasureChestIgnoreList);
        Assert.Single(active.PreferredBaits);
        Assert.Single(active.PreferredTackles);
        Assert.Single(active.PreferredSecondTackles);
        Assert.NotSame(active.PreferredBaits, draft.PreferredBaits);
        Assert.NotSame(active.PreferredTackles, draft.PreferredTackles);
        Assert.NotSame(active.PreferredSecondTackles, draft.PreferredSecondTackles);
        Assert.Equal(active.MinigameAssistance, draft.MinigameAssistance);
        Assert.Equal(active.FishSpeedPercent, draft.FishSpeedPercent);
        Assert.Equal(active.ProgressGainPercent, draft.ProgressGainPercent);
        Assert.Equal(active.ProgressLossPercent, draft.ProgressLossPercent);
        Assert.Equal(active.TreasureSpeedPercent, draft.TreasureSpeedPercent);
        Assert.Equal(active.BarSizePercent, draft.BarSizePercent);
        Assert.Equal(active.EnableAutomationButton.ToString(), draft.EnableAutomationButton.ToString());
        Assert.NotSame(active.EnableAutomationButton, draft.EnableAutomationButton);
        Assert.Equal(active.ToggleTreasureTargetingButton.ToString(), draft.ToggleTreasureTargetingButton.ToString());
        Assert.NotSame(active.ToggleTreasureTargetingButton, draft.ToggleTreasureTargetingButton);
    }
}
