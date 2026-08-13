using FishingAssistant.Configuration;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SObject = StardewValley.Object;

namespace FishingAssistant.Inventory;

internal sealed class AutoTrashService(IMonitor monitor, Func<string, string> translate)
{
    public void OnInventoryChanged(
        InventoryChangedEventArgs eventArgs,
        ModConfig config,
        bool automationEnabled)
    {
        if (!eventArgs.IsLocalPlayer || !ReferenceEquals(eventArgs.Player, Game1.player))
            return;

        Dictionary<Item, int> acquired = new(ReferenceEqualityComparer.Instance);
        foreach (Item item in eventArgs.Added)
            AddQuantity(item, item.Stack);
        foreach (ItemStackSizeChange change in eventArgs.QuantityChanged)
            AddQuantity(change.Item, change.NewSize - change.OldSize);

        foreach ((Item item, int acquiredQuantity) in acquired)
        {
            int inventoryIndex = eventArgs.Player.Items.IndexOf(item);
            if (inventoryIndex < 0 || item.Stack <= 0)
                continue;

            AutoTrashDecision decision = AutoTrashPolicy.Decide(new AutoTrashConditions(
                automationEnabled,
                config.AutoTrashJunk,
                item.QualifiedItemId,
                item.canBeTrashed(),
                item.Category == SObject.FishCategory || item.HasContextTag("category_fish"),
                config.AllowTrashFish,
                acquiredQuantity,
                item.Stack,
                config.JunkList,
                config.JunkIgnoreList));
            if (!decision.ShouldTrash)
                continue;

            Item discarded = item.getOne();
            discarded.Stack = decision.Quantity;
            if (decision.Quantity >= item.Stack)
                eventArgs.Player.removeItemFromInventory(item);
            else
                item.Stack -= decision.Quantity;

            Utility.trashItem(discarded);
            Game1.addHUDMessage(new HUDMessage(string.Format(
                translate("hud.auto_trash.discarded"),
                discarded.DisplayName,
                decision.Quantity)));
            monitor.Log(
                $"Automatically trashed {decision.Quantity} {discarded.DisplayName} " +
                $"for local screen {Context.ScreenId}; only the newly acquired quantity was removed.",
                LogLevel.Trace);
        }

        return;

        void AddQuantity(Item item, int quantity)
        {
            if (quantity <= 0)
                return;

            acquired[item] = acquired.GetValueOrDefault(item) + quantity;
        }
    }
}
