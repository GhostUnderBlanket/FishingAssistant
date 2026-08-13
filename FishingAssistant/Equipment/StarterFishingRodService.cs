using FishingAssistant.Configuration;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace FishingAssistant.Equipment;

internal enum FishingRodGrantResult
{
    Granted,
    AlreadyOwned,
    InventoryFull,
    InvalidItem,
    WorldNotReady
}

internal sealed class StarterFishingRodService(IMonitor monitor)
{
    public FishingRodGrantResult EnsureRod(string itemId)
    {
        if (!Context.IsWorldReady)
            return FishingRodGrantResult.WorldNotReady;

        Farmer player = Game1.player;
        if (player.Items.OfType<FishingRod>().Any())
            return FishingRodGrantResult.AlreadyOwned;

        Item item = ItemRegistry.Create(itemId);
        if (item is not FishingRod rod)
        {
            monitor.Log($"Couldn't create the configured fishing rod '{itemId}'.", LogLevel.Error);
            return FishingRodGrantResult.InvalidItem;
        }

        if (!player.couldInventoryAcceptThisItem(rod))
        {
            monitor.Log(
                $"Couldn't add the configured starter fishing rod for local screen {Context.ScreenId} because the inventory is full.",
                LogLevel.Warn);
            return FishingRodGrantResult.InventoryFull;
        }

        if (!player.addItemToInventoryBool(rod))
        {
            monitor.Log($"Couldn't add the configured starter fishing rod for local screen {Context.ScreenId}.", LogLevel.Warn);
            return FishingRodGrantResult.InventoryFull;
        }

        Game1.addHUDMessage(HUDMessage.ForItemGained(rod, 1));
        monitor.Log(
            $"Added {rod.DisplayName} for local screen {Context.ScreenId} because the player had no fishing rod.",
            LogLevel.Info);
        return FishingRodGrantResult.Granted;
    }
}
