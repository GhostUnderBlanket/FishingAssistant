using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace FishingAssistant.Fishing;

internal enum TreasureCollectResult
{
    Collected,
    PartiallyCollected,
    NoCapacity,
    Empty
}

internal sealed class FishingTreasureMenuAdapter(Farmer player, ItemGrabMenu menu)
{
    public static FishingTreasureMenuAdapter? ForCurrentScreen()
    {
        return Context.IsWorldReady
            && Game1.activeClickableMenu is ItemGrabMenu
            {
                source: ItemGrabMenu.source_fishingChest,
                context: FishingRod
            } menu
            ? new FishingTreasureMenuAdapter(Game1.player, menu)
            : null;
    }

    public object Identity => menu;

    public bool IsPlayerHoldingItem => menu.heldItem is not null;

    public bool HasRemainingItems => menu.ItemsToGrabMenu.actualInventory.Any(item => item is not null);

    public bool HasUnblockedItem(ISet<Item> blockedItems)
    {
        return menu.ItemsToGrabMenu.actualInventory.Any(item => item is not null && !blockedItems.Contains(item));
    }

    public TreasureCollectResult TryCollectNext(ISet<Item> blockedItems)
    {
        IList<Item> items = menu.ItemsToGrabMenu.actualInventory;
        for (int index = 0; index < items.Count; index++)
        {
            Item? item = items[index];
            if (item is null || blockedItems.Contains(item))
                continue;

            int originalStack = item.Stack;
            Item? remainder = player.addItemToInventory(item);
            if (remainder is null)
            {
                items[index] = null!;
                Game1.playSound("coin");
                return TreasureCollectResult.Collected;
            }

            items[index] = remainder;
            if (remainder.Stack < originalStack)
            {
                Game1.playSound("coin");
                return TreasureCollectResult.PartiallyCollected;
            }

            blockedItems.Add(remainder);
            return TreasureCollectResult.NoCapacity;
        }

        return this.HasRemainingItems ? TreasureCollectResult.NoCapacity : TreasureCollectResult.Empty;
    }

    public void Close()
    {
        menu.exitThisMenu();
    }

    public void DropRemainingItems()
    {
        menu.DropRemainingItems();
        menu.exitThisMenu();
    }

    public void DiscardRemainingItems()
    {
        menu.ItemsToGrabMenu.actualInventory.Clear();
        menu.exitThisMenu();
    }
}
