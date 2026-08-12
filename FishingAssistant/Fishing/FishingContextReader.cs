using FishingAssistant.Runtime;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace FishingAssistant.Fishing;

internal static class FishingContextReader
{
    public static FishingObservation Read(bool isEnabled)
    {
        if (!Context.IsWorldReady)
            return new FishingObservation(isEnabled, IsWorldReady: false, HasFishingRod: false);

        FishingRod? rod = Game1.player.CurrentTool as FishingRod;
        bool isMinigame = Game1.activeClickableMenu is BobberBar;
        bool isTreasureMenu = rod?.showingTreasure == true && Game1.activeClickableMenu is ItemGrabMenu;
        bool hasBlockingMenu = Game1.activeClickableMenu is not null && !isMinigame && !isTreasureMenu;

        return new FishingObservation(
            IsEnabled: isEnabled,
            IsWorldReady: true,
            HasFishingRod: rod is not null,
            HasBlockingMenu: hasBlockingMenu,
            IsTimingCast: rod?.isTimingCast == true,
            IsCasting: rod?.isCasting == true,
            IsBobberInAir: rod?.castedButBobberStillInAir == true,
            IsFishing: rod?.isFishing == true,
            IsNibbling: rod?.isNibbling == true,
            IsReeling: rod?.isReeling == true,
            IsFishCaught: rod?.fishCaught == true,
            IsPullingOutOfWater: rod?.pullingOutOfWater == true,
            IsMinigame: isMinigame,
            IsTreasureMenu: isTreasureMenu
        );
    }
}
