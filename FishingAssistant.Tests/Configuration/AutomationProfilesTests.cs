using FishingAssistant.Configuration;

namespace FishingAssistant.Tests.Configuration;

public sealed class AutomationProfilesTests
{
    [Fact]
    public void Relaxed_AutomatesTheWholeCoreLoop()
    {
        ModConfig config = new() { AutomaticBubbleSteering = true };

        AutomationProfiles.Apply(config, AutomationProfile.Relaxed);

        Assert.True(config.AutoCastFishingRod);
        Assert.True(config.AutoHookFish);
        Assert.True(config.AutoPlayMiniGame);
        Assert.True(config.AutoClosePopup);
        Assert.True(config.AutoLootTreasure);
        Assert.True(config.DisplayFishPreview);
        Assert.True(config.AutomaticBubbleSteering);
    }

    [Fact]
    public void Training_LeavesTheMinigameToThePlayer()
    {
        ModConfig config = new();

        AutomationProfiles.Apply(config, AutomationProfile.Training);

        Assert.True(config.AutoCastFishingRod);
        Assert.True(config.AutoHookFish);
        Assert.False(config.AutoPlayMiniGame);
        Assert.True(config.AutoClosePopup);
        Assert.True(config.AutoLootTreasure);
        Assert.True(config.AutomaticBubbleSteering);
    }

    [Fact]
    public void ManualPlus_LeavesCoreFishingManualAndEnablesQualityOfLife()
    {
        ModConfig config = new();

        AutomationProfiles.Apply(config, AutomationProfile.ManualPlus);

        Assert.False(config.AutoCastFishingRod);
        Assert.False(config.AutoHookFish);
        Assert.False(config.AutoPlayMiniGame);
        Assert.True(config.AutoClosePopup);
        Assert.True(config.AutoLootTreasure);
        Assert.True(config.AutomaticBubbleSteering);
        Assert.True(config.DisplayFishPreview);
    }

    [Fact]
    public void ApplyingAProfile_DoesNotChangeUnrelatedChoices()
    {
        ModConfig config = new()
        {
            JunkDisposalMode = JunkDisposalMode.WhenInventoryFull,
            InstantFishBite = true,
            TreasureChance = TreasureChanceBehavior.Always,
            AlwaysPerfect = true
        };

        AutomationProfiles.Apply(config, AutomationProfile.Training);

        Assert.Equal(JunkDisposalMode.WhenInventoryFull, config.JunkDisposalMode);
        Assert.True(config.InstantFishBite);
        Assert.Equal(TreasureChanceBehavior.Always, config.TreasureChance);
        Assert.True(config.AlwaysPerfect);
    }

    [Fact]
    public void MarkCustom_PreservesEveryOption()
    {
        ModConfig config = new() { AutoCastFishingRod = false };

        AutomationProfiles.MarkCustom(config);

        Assert.Equal(AutomationProfile.Custom, config.AutomationProfile);
        Assert.False(config.AutoCastFishingRod);
    }
}
