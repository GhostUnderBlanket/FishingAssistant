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

    public bool HasCollectibleItem(ISet<Item> blockedItems, IReadOnlySet<string> ignoredItemIds)
    {
        return menu.ItemsToGrabMenu.actualInventory.Any(item => item is not null
            && !blockedItems.Contains(item)
            && !ignoredItemIds.Contains(item.QualifiedItemId));
    }

    public bool HasBlockedNonIgnoredItem(ISet<Item> blockedItems, IReadOnlySet<string> ignoredItemIds) =>
        menu.ItemsToGrabMenu.actualInventory.Any(item => item is not null
            && blockedItems.Contains(item)
            && !ignoredItemIds.Contains(item.QualifiedItemId));

    public bool HasIgnoredItem(IReadOnlySet<string> ignoredItemIds) =>
        menu.ItemsToGrabMenu.actualInventory.Any(item => item is not null
            && ignoredItemIds.Contains(item.QualifiedItemId));

    public TreasureCollectResult TryCollectNext(ISet<Item> blockedItems, IReadOnlySet<string> ignoredItemIds)
    {
        IList<Item> items = menu.ItemsToGrabMenu.actualInventory;
        for (int index = 0; index < items.Count; index++)
        {
            Item? item = items[index];
            if (item is null || blockedItems.Contains(item) || ignoredItemIds.Contains(item.QualifiedItemId))
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

    public void DropBlockedItems(ISet<Item> blockedItems, IReadOnlySet<string> ignoredItemIds)
    {
        IList<Item> items = menu.ItemsToGrabMenu.actualInventory;
        for (int index = 0; index < items.Count; index++)
        {
            Item? item = items[index];
            if (item is null || !blockedItems.Contains(item) || ignoredItemIds.Contains(item.QualifiedItemId))
                continue;

            items[index] = null!;
            Game1.createItemDebris(item, player.getStandingPosition(), player.FacingDirection);
        }
    }

    public void DiscardBlockedItems(ISet<Item> blockedItems, IReadOnlySet<string> ignoredItemIds)
    {
        IList<Item> items = menu.ItemsToGrabMenu.actualInventory;
        for (int index = 0; index < items.Count; index++)
        {
            Item? item = items[index];
            if (item is not null && blockedItems.Contains(item) && !ignoredItemIds.Contains(item.QualifiedItemId))
                items[index] = null!;
        }
    }

    public void DiscardRemainingItems()
    {
        menu.ItemsToGrabMenu.actualInventory.Clear();
        menu.exitThisMenu();
    }
}
