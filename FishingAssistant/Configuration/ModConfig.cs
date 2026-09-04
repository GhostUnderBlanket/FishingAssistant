using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace FishingAssistant.Configuration;

internal sealed class ModConfig
{
    internal const int CurrentVersion = 29;
    internal const int MaximumQuickControlSlots = 5;
    internal const string DefaultStarterRod = "None";

    internal static readonly IReadOnlyList<string> DefaultJunkList =
    [
        "(O)168",
        "(O)169",
        "(O)170",
        "(O)171",
        "(O)172"
    ];

    public int ConfigVersion { get; set; } = CurrentVersion;

    public KeybindList EnableAutomationButton { get; set; } = new(SButton.F5);

    public KeybindList EnableAutomationOptionalButton { get; set; } = new(SButton.None);

    public KeybindList OpenConfigMenuButton { get; set; } = new(SButton.F6);

    public KeybindList OpenConfigMenuOptionalButton { get; set; } = new(SButton.ControllerBack);

    public KeybindList ToggleTreasureTargetingButton { get; set; } = new(SButton.None);

    public KeybindList ToggleTreasureTargetingOptionalButton { get; set; } = new(SButton.None);

    public HudPosition ModStatusPosition { get; set; } = HudPosition.Left;

    public HudVisibilityMode HudVisibility { get; set; } = HudVisibilityMode.WhileFishing;

    public List<QuickControlAction> QuickControlActions { get; set; } =
    [
        QuickControlAction.ToggleAutomation,
        QuickControlAction.ToggleTreasureTargeting,
        QuickControlAction.ToggleAutoEatFood
    ];

    public AutomationProfile AutomationProfile { get; set; } = AutomationProfile.Relaxed;

    public bool AutoCastFishingRod { get; set; } = true;

    public bool AutoHookFish { get; set; } = true;

    public bool AutoPlayMiniGame { get; set; } = true;

    public bool AutoClosePopup { get; set; } = true;

    public bool AutoLootTreasure { get; set; } = true;

    public AutomationTimingPreset AutomationTiming { get; set; } = AutomationTimingPreset.Normal;

    public float AutoCastDelaySeconds { get; set; } = 1f;

    public float CatchPopupDurationSeconds { get; set; } = 1.5f;

    public float TreasureLootDelaySeconds { get; set; } = 0.5f;

    public InventoryFullAction ActionIfInventoryFull { get; set; } = InventoryFullAction.Stop;

    public List<string> TreasureChestIgnoreList { get; set; } = [];

    public bool IgnoreJunkListItemsInTreasureChests { get; set; }

    public IgnoredTreasureAction ActionIfOnlyIgnoredTreasureRemains { get; set; } = IgnoredTreasureAction.KeepOpen;

    public JunkDisposalMode JunkDisposalMode { get; set; } = JunkDisposalMode.WhenInventoryFull;

    public bool AutoTrashJunk { get; set; }

    public bool ShouldSerializeAutoTrashJunk() => false;

    public List<string> JunkList { get; set; } = [.. DefaultJunkList];

    public List<string> JunkIgnoreList { get; set; } = [];

    public bool ShouldSerializeJunkIgnoreList() => false;

    public PauseFishingBehavior AutoPauseFishing { get; set; } = PauseFishingBehavior.WarnAndPause;

    public int TimeToPause { get; set; } = 24;

    public int WarnCount { get; set; } = 1;

    public bool OpenInventoryOnStop { get; set; } = true;

    public bool AutoEatFood { get; set; }

    public int EnergyPercentToEat { get; set; } = 5;

    public float FoodConsumptionDelaySeconds { get; set; } = 1f;

    public AutoEatTriggerBehavior AutoEatTrigger { get; set; } = AutoEatTriggerBehavior.BeforeNextCast;

    public bool AllowEatingFish { get; set; }

    public List<string> PreferredFoods { get; set; } = [];

    public FoodFallbackBehavior FoodFallback { get; set; } = FoodFallbackBehavior.BestValue;

    public bool AutoAttachBait { get; set; }

    public string PreferredBait { get; set; } = "Any";

    public bool ShouldSerializePreferredBait() => false;

    public List<string> PreferredBaits { get; set; } = [];

    public bool SpawnBaitIfDontHave { get; set; }

    public int BaitAmountToSpawn { get; set; } = 10;

    public bool AutoAttachTackles { get; set; }

    public string PreferredTackle { get; set; } = "Any";

    public bool ShouldSerializePreferredTackle() => false;

    public List<string> PreferredTackles { get; set; } = [];

    public string PreferredAdvIridiumTackle { get; set; } = "Any";

    public bool ShouldSerializePreferredAdvIridiumTackle() => false;

    public List<string> PreferredSecondTackles { get; set; } = [];

    public bool SpawnTackleIfDontHave { get; set; }

    public SkipMinigameBehavior SkipFishingMiniGame { get; set; } = SkipMinigameBehavior.Off;

    public int SkipMinigameCatchesRequired { get; set; } = 1;

    public bool InstantFishBite { get; set; }

    public bool ShouldSerializeInstantFishBite() => false;

    public int BiteWaitingTimePercent { get; set; } = 100;

    public bool AutomaticBubbleSteering { get; set; } = true;

    public bool AutomaticCastPowerAdjustment { get; set; }

    public bool ShouldSerializeAutomaticCastPowerAdjustment() => false;

    public CastPowerAdjustmentMode AutomaticCastPowerAdjustmentMode { get; set; }

    public SteeringEffort SteeringEffort { get; set; } = SteeringEffort.Normal;

    public bool ShowFishingBubbleMarker { get; set; }

    public int PreferFishAmount { get; set; } = 1;

    public FishAmountBehavior FishAmountBehavior { get; set; } = FishAmountBehavior.Vanilla;

    public FishQualityPreference PreferFishQuality { get; set; } = FishQualityPreference.Any;

    public FishQualityBehavior FishQualityBehavior { get; set; } = FishQualityBehavior.Vanilla;

    public bool AlwaysPerfect { get; set; }

    public bool AlwaysMaxFishSize { get; set; }

    public MinigameAssistancePreset MinigameAssistance { get; set; } = MinigameAssistancePreset.Off;

    public int FishSpeedPercent { get; set; } = 100;

    public int ProgressGainPercent { get; set; } = 100;

    public int ProgressLossPercent { get; set; } = 100;

    public int TreasureSpeedPercent { get; set; } = 100;

    public int BarSizePercent { get; set; } = 100;

    public float FishDifficultyMultiplier { get; set; } = 1f;

    public bool ShouldSerializeFishDifficultyMultiplier() => false;

    public int FishDifficultyAdditive { get; set; }

    public bool ShouldSerializeFishDifficultyAdditive() => false;

    public bool InstantCatchTreasure { get; set; }

    public bool TreasureTargeting { get; set; }

    public TreasureChanceBehavior TreasureChance { get; set; } = TreasureChanceBehavior.Default;

    public bool ShouldSerializeTreasureChance() => false;

    public int TreasureChancePercent { get; set; } = 15;

    public TreasureChanceBehavior GoldenTreasureChance { get; set; } = TreasureChanceBehavior.Default;

    public bool ShouldSerializeGoldenTreasureChance() => false;

    public int GoldenTreasureChancePercent { get; set; } = 25;

    public bool DisplayFishPreview { get; set; } = true;

    public FishPreviewStyle FishPreviewStyle { get; set; } = FishPreviewStyle.Sonar;

    public bool ShowFishName { get; set; } = true;

    public bool ShowTreasure { get; set; } = true;

    public bool ShowUncaughtFish { get; set; }

    public bool ShowLegendaryFish { get; set; }

    public string StartWithFishingRod { get; set; } = DefaultStarterRod;

    public int DefaultCastPower { get; set; } = 100;

    public float UnlockCastPowerTime { get; set; } = 1f;

    public bool InfiniteBait { get; set; }

    public bool InfiniteTackle { get; set; }

    public bool AddAutoHookEnchantment { get; set; }

    public bool AddEfficientEnchantment { get; set; }

    public bool AddMasterEnchantment { get; set; }

    public bool AddPreservingEnchantment { get; set; }

    public bool RemoveWhenUnequipped { get; set; } = true;

    internal ModConfig CreateDraft()
    {
        ModConfig draft = (ModConfig)this.MemberwiseClone();
        draft.EnableAutomationButton = KeybindList.Parse(this.EnableAutomationButton.ToString());
        draft.EnableAutomationOptionalButton =
            KeybindList.Parse(this.EnableAutomationOptionalButton.ToString());
        draft.OpenConfigMenuButton = KeybindList.Parse(this.OpenConfigMenuButton.ToString());
        draft.OpenConfigMenuOptionalButton =
            KeybindList.Parse(this.OpenConfigMenuOptionalButton.ToString());
        draft.ToggleTreasureTargetingButton = KeybindList.Parse(this.ToggleTreasureTargetingButton.ToString());
        draft.ToggleTreasureTargetingOptionalButton =
            KeybindList.Parse(this.ToggleTreasureTargetingOptionalButton.ToString());
        draft.JunkList = [.. this.JunkList];
        draft.JunkIgnoreList = [.. this.JunkIgnoreList];
        draft.TreasureChestIgnoreList = [.. this.TreasureChestIgnoreList];
        draft.PreferredFoods = [.. this.PreferredFoods];
        draft.PreferredBaits = [.. this.PreferredBaits];
        draft.PreferredTackles = [.. this.PreferredTackles];
        draft.PreferredSecondTackles = [.. this.PreferredSecondTackles];
        draft.QuickControlActions = [.. this.QuickControlActions];
        return draft;
    }
}
