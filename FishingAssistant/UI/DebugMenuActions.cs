#if FISHING_ASSISTANT_TEST_BUILD
namespace FishingAssistant.UI;

internal sealed record DebugMenuActions(
    Action SetLowEnergy,
    Action RestoreEnergy,
    Action WarpToBeachFishingSpot,
    Action<int> CreateFishingBubble,
    Action PrepareIceFishingFestival,
    Action PrepareStardewValleyFair);
#endif
