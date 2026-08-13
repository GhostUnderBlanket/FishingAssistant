using Microsoft.Xna.Framework;
using StardewValley;

namespace FishingAssistant.Debugging;

internal static class DebugFishingBubblePolicy
{
    public static Point? FindTarget(Point landing, int facingDirection, Func<int, int, bool> isFishable)
    {
        ArgumentNullException.ThrowIfNull(isFishable);
        bool horizontalCast = facingDirection is Game1.left or Game1.right;
        Point[] offsets = horizontalCast
            ? [new(0, 1), new(0, -1), Point.Zero]
            : [new(1, 0), new(-1, 0), Point.Zero];

        foreach (Point offset in offsets)
        {
            Point candidate = new(landing.X + offset.X, landing.Y + offset.Y);
            if (isFishable(candidate.X, candidate.Y))
                return candidate;
        }

        return null;
    }
}
