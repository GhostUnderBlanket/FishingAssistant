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

        if (config.JunkDisposalMode == JunkDisposalMode.WhenInventoryFull)
        {
            this.TryDiscardBatchIfFull(eventArgs.Player, config, automationEnabled);
            return;
        }

        if (!automationEnabled || config.JunkDisposalMode != JunkDisposalMode.Immediately)
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
                true,
                item.QualifiedItemId,
                item.canBeTrashed(),
                IsFish(item),
                config.AllowTrashFish,
                acquiredQuantity,
                item.Stack,
                config.JunkList));
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

    public bool TryDiscardBatchIfFull(
        Farmer player,
        ModConfig config,
        bool automationEnabled)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(config);

        if (!Context.IsWorldReady
            || !ReferenceEquals(player, Game1.player)
            || !player.IsLocalPlayer
            || !automationEnabled
            || config.JunkDisposalMode != JunkDisposalMode.WhenInventoryFull
            || !IsInventoryFull(player))
        {
            return false;
        }

        List<BatchJunkCandidate> candidates = [];
        for (int index = 0; index < player.MaxItems; index++)
        {
            if (player.Items[index] is not { } item)
                continue;

            candidates.Add(new BatchJunkCandidate(
                index,
                item.QualifiedItemId,
                item.canBeTrashed(),
                IsFish(item),
                item.Stack));
        }

        IReadOnlyList<int> selected = BatchJunkDisposalPolicy.Select(new(
            automationEnabled,
            config.JunkDisposalMode,
            true,
            config.AllowTrashFish,
            config.JunkList,
            candidates));
        if (selected.Count == 0)
            return false;

        List<Item> discarded = [];
        foreach (int index in selected)
        {
            if (index < player.Items.Count && player.Items[index] is { } item)
                discarded.Add(item);
        }

        if (discarded.Count == 0)
            return false;

        int totalQuantity = 0;
        int reclaimedMoney = 0;
        foreach (Item item in discarded)
        {
            totalQuantity += item.Stack;
            int reclamation = Utility.getTrashReclamationPrice(item, player);
            if (reclamation > 0)
                reclaimedMoney += reclamation;
            if (item is SObject && player.specialItems.Contains(item.ItemId))
                player.specialItems.Remove(item.ItemId);
            player.removeItemFromInventory(item);
        }

        player.Money += reclaimedMoney;
        Game1.playSound("trashcan");
        Game1.addHUDMessage(new HUDMessage(string.Format(
            translate("hud.junk_disposal.batch"),
            totalQuantity,
            discarded.Count)));
        monitor.Log(
            $"Automatically trashed {totalQuantity} item(s) across {discarded.Count} Junk List " +
            $"stack(s) after the inventory became full on local screen {Context.ScreenId}.",
            LogLevel.Trace);
        return true;
    }

    private static bool IsInventoryFull(Farmer player)
    {
        for (int index = 0; index < player.MaxItems; index++)
        {
            if (index >= player.Items.Count || player.Items[index] is null)
                return false;
        }

        return true;
    }

    private static bool IsFish(Item item)
    {
        return item.Category == SObject.FishCategory || item.HasContextTag("category_fish");
    }
}
