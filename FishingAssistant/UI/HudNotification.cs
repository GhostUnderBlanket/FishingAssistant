using StardewValley;

namespace FishingAssistant.UI;

internal static class HudNotification
{
    public static void ShowItem(string message, Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        HUDMessage notification = new(message, HUDMessage.newQuest_type)
        {
            messageSubject = item.getOne()
        };
        Game1.addHUDMessage(notification);
    }
}
