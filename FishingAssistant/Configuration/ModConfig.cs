using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace FishingAssistant.Configuration;

internal sealed class ModConfig
{
    internal const int CurrentVersion = 15;
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

    public KeybindList OpenConfigMenuButton { get; set; } = new(SButton.F6);

    public KeybindList ToggleTreasureTargetingButton { get; set; } = new(SButton.None);

    public HudPosition ModStatusPosition { get; set; } = HudPosition.Left;

    public AutomationProfile AutomationProfile { get; set; } = AutomationProfile.Relaxed;

    public bool AutoCastFishingRod { get; set; } = true;

    public bool AutoHookFish { get; set; } = true;

    public bool AutoPlayMiniGame { get; set; } = true;

    public bool AutoClosePopup { get; set; } = true;

    public bool AutoLootTreasure { get; set; } = true;

    public InventoryFullAction ActionIfInventoryFull { get; set; } = InventoryFullAction.Stop;

    public List<string> TreasureChestIgnoreList { get; set; } = [];

    public bool IgnoreJunkListItemsInTreasureChests { get; set; }

    public IgnoredTreasureAction ActionIfOnlyIgnoredTreasureRemains { get; set; } = IgnoredTreasureAction.KeepOpen;

    public JunkDisposalMode JunkDisposalMode { get; set; } = JunkDisposalMode.WhenInventoryFull;

    public bool AutoTrashJunk { get; set; }

    public bool ShouldSerializeAutoTrashJunk() => false;

    public bool AllowTrashFish { get; set; }

    public List<string> JunkList { get; set; } = [.. DefaultJunkList];

    public List<string> JunkIgnoreList { get; set; } = [];

    public bool ShouldSerializeJunkIgnoreList() => false;

    public PauseFishingBehavior AutoPauseFishing { get; set; } = PauseFishingBehavior.WarnAndPause;

    public int TimeToPause { get; set; } = 24;

    public int WarnCount { get; set; } = 1;

    public bool AutoEatFood { get; set; }

    public int EnergyPercentToEat { get; set; } = 5;

    public bool AllowEatingFish { get; set; }

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

    public bool InstantFishBite { get; set; }

    public bool AutomaticBubbleSteering { get; set; } = true;

    public int PreferFishAmount { get; set; } = 1;

    public FishQualityPreference PreferFishQuality { get; set; } = FishQualityPreference.Any;

    public bool AlwaysPerfect { get; set; }

    public bool AlwaysMaxFishSize { get; set; }

    public float FishDifficultyMultiplier { get; set; } = 1f;

    public int FishDifficultyAdditive { get; set; }

    public bool InstantCatchTreasure { get; set; }

    public bool TreasureTargeting { get; set; }

    public TreasureChanceBehavior TreasureChance { get; set; } = TreasureChanceBehavior.Default;

    public TreasureChanceBehavior GoldenTreasureChance { get; set; } = TreasureChanceBehavior.Default;

    public bool DisplayFishPreview { get; set; } = true;

    public FishPreviewStyle FishPreviewStyle { get; set; } = FishPreviewStyle.Sonar;

    public bool ShowFishName { get; set; } = true;

    public bool ShowTreasure { get; set; } = true;

    public bool ShowUncaughtFish { get; set; }

    public bool ShowLegendaryFish { get; set; }

    public string StartWithFishingRod { get; set; } = DefaultStarterRod;

    public int DefaultCastPower { get; set; } = 100;

    public float AutoCastDelaySeconds { get; set; } = 1f;

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
        draft.OpenConfigMenuButton = KeybindList.Parse(this.OpenConfigMenuButton.ToString());
        draft.ToggleTreasureTargetingButton = KeybindList.Parse(this.ToggleTreasureTargetingButton.ToString());
        draft.JunkList = [.. this.JunkList];
        draft.JunkIgnoreList = [.. this.JunkIgnoreList];
        draft.TreasureChestIgnoreList = [.. this.TreasureChestIgnoreList];
        draft.PreferredBaits = [.. this.PreferredBaits];
        draft.PreferredTackles = [.. this.PreferredTackles];
        draft.PreferredSecondTackles = [.. this.PreferredSecondTackles];
        return draft;
    }
}
