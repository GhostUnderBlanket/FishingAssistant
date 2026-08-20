using StardewValley;
using StardewValley.Minigames;

namespace FishingAssistant.Fishing;

internal static class FestivalFishingContext
{
    public static bool IsSupportedFishingActivity
    {
        get
        {
            // Festival fishing minigames use FishingGame. Ice Fishing is the
            // exception: vanilla keeps it in the festival event and allows
            // normal FishingRod use while its competition timer is running.
            if (Game1.currentMinigame is FishingGame { gameDone: false })
                return true;

            Event? festival = Game1.CurrentEvent;
            return FestivalFishingPolicy.IsIceFishingCompetitionActive(
                festival?.isSpecificFestival("winter8") == true,
                festival?.festivalTimer ?? 0);
        }
    }
}
