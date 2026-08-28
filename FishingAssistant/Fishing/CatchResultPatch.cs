using FishingAssistant.Configuration;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Tools;

namespace FishingAssistant.Fishing;

internal static class CatchResultPatch
{
    private static Func<ModConfig>? getConfig;
    private static PerfectCatchProgressService? perfectCatchProgress;
    private static Func<bool>? isSkippedMinigameResult;
    private static IMonitor? monitor;

    public static void Apply(
        Harmony harmony,
        Func<ModConfig> configProvider,
        PerfectCatchProgressService progressService,
        Func<bool> skippedMinigameResultProvider,
        IMonitor modMonitor)
    {
        getConfig = configProvider;
        perfectCatchProgress = progressService;
        isSkippedMinigameResult = skippedMinigameResultProvider;
        monitor = modMonitor;

        harmony.Patch(
            AccessTools.Method(typeof(FishingRod), nameof(FishingRod.pullFishFromWater)),
            prefix: new HarmonyMethod(typeof(CatchResultPatch), nameof(BeforePullFishFromWater)));
    }

    private static void BeforePullFishFromWater(
        string fishId,
        ref int fishSize,
        ref int fishQuality,
        ref bool wasPerfect,
        bool fromFishPond,
        bool isBossFish,
        ref int numCaught)
    {
        try
        {
            if (getConfig is null)
                return;

            BobberBar? bar = Game1.activeClickableMenu as BobberBar;
            bool belongsToActiveBar = bar is not null
                && string.Equals(bar.whichFish, fishId, StringComparison.Ordinal);
            int maximumFishSize = belongsToActiveBar ? bar!.maxFishSize : -1;
            bool usesChallengeBait = belongsToActiveBar && bar!.challengeBaitFishes > -1;
            bool isFish = belongsToActiveBar
                && DataLoader.Fish(Game1.content).ContainsKey(bar!.whichFish);
            bool isFestivalFishing = Game1.isFestival() || Game1.currentMinigame is FishingGame;
            ModConfig config = getConfig();
            bool genuinePerfect = wasPerfect
                && belongsToActiveBar
                && isFish
                && !fromFishPond
                && !isFestivalFishing
                && !(isSkippedMinigameResult?.Invoke() ?? false);
            int genuineCatchQuantity = Math.Max(1, numCaught);

            if (genuinePerfect && perfectCatchProgress is not null)
            {
                string qualifiedFishId = ItemRegistry.GetMetadata(fishId)?.QualifiedItemId ?? fishId;
                perfectCatchProgress.Record(Game1.player, qualifiedFishId, genuineCatchQuantity);
            }

            CatchResultDecision decision = CatchResultPolicy.Decide(new CatchResultConditions(
                fishSize,
                maximumFishSize,
                fishQuality,
                wasPerfect,
                numCaught,
                config.PreferFishAmount,
                config.PreferFishQuality,
                config.AlwaysPerfect,
                config.AlwaysMaxFishSize,
                isFish,
                isFestivalFishing,
                fromFishPond,
                isBossFish,
                usesChallengeBait));

            fishSize = decision.FishSize;
            fishQuality = decision.FishQuality;
            wasPerfect = decision.IsPerfect;
            numCaught = decision.FishCount;

            if (decision.WasChanged)
            {
                monitor?.Log(
                    $"Adjusted catch result for local screen {Context.ScreenId}: " +
                    $"size={decision.FishSize}, quality={decision.FishQuality}, " +
                    $"perfect={decision.IsPerfect}, count={decision.FishCount}.",
                    LogLevel.Trace);
            }
        }
        catch (Exception exception)
        {
            // A compatibility failure must never prevent vanilla from completing a
            // catch. Harmony will continue with the original arguments.
            monitor?.Log($"Catch-result assistance was skipped because its compatibility boundary failed.\n{exception}",
                LogLevel.Error);
        }
    }
}
