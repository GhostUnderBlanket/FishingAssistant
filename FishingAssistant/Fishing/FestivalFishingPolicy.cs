namespace FishingAssistant.Fishing;

internal static class FestivalFishingPolicy
{
    public static bool IsIceFishingCompetitionActive(bool isIceFishingFestival, int festivalTimer)
    {
        return isIceFishingFestival && festivalTimer > 0;
    }
}
