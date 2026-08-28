using System.Globalization;
using StardewValley;

namespace FishingAssistant.Fishing;

internal sealed class PerfectCatchProgressService
{
    private const string KeyPrefix = "ChibiKyu.FishingAssistant2/PerfectCatches/";

    public int GetCount(Farmer player, string qualifiedFishId)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (string.IsNullOrWhiteSpace(qualifiedFishId)
            || !player.modData.TryGetValue(KeyPrefix + qualifiedFishId, out string? rawCount)
            || !int.TryParse(rawCount, NumberStyles.None, CultureInfo.InvariantCulture, out int count))
        {
            return 0;
        }

        return Math.Max(0, count);
    }

    public void Record(Farmer player, string qualifiedFishId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (string.IsNullOrWhiteSpace(qualifiedFishId) || quantity <= 0)
            return;

        int current = this.GetCount(player, qualifiedFishId);
        int updated = current > int.MaxValue - quantity ? int.MaxValue : current + quantity;
        player.modData[KeyPrefix + qualifiedFishId] = updated.ToString(CultureInfo.InvariantCulture);
    }
}
