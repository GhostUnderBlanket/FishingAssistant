using FishingAssistant.Configuration;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace FishingAssistant.Equipment;

internal sealed class BaitAttachmentService(IMonitor monitor, Func<string, string> translate)
{
    public void UpdateCurrent(ModConfig config)
    {
        if (!Context.IsWorldReady || Game1.player.CurrentTool is not FishingRod rod)
            return;

        SObject? attached = rod.GetBait();
        List<BaitInventoryCandidate> candidates = Game1.player.Items
            .Select((item, index) => (item, index))
            .Where(entry => entry.item is SObject { Category: SObject.baitCategory })
            .Select(entry => new BaitInventoryCandidate(entry.index, entry.item!.QualifiedItemId))
            .ToList();
        BaitAttachmentConditions conditions = new(
            config.AutoAttachBait,
            this.IsSafeToAttach(rod),
            rod.CanUseBait(),
            attached?.QualifiedItemId,
            attached?.getRemainingStackSpace() ?? 0,
            config.PreferredBait,
            config.SpawnBaitIfDontHave,
            candidates
        );
        BaitAttachmentDecision decision = BaitAttachmentPolicy.Decide(conditions);
        switch (decision.Action)
        {
            case BaitAttachmentAction.AttachFromInventory:
            case BaitAttachmentAction.RefillFromInventory:
                this.AttachFromInventory(rod, decision.InventoryIndex, decision.Action);
                break;
            case BaitAttachmentAction.Spawn:
                this.SpawnAndAttach(rod, decision.SpawnItemId!, config.BaitAmountToSpawn);
                break;
        }
    }

    private bool IsSafeToAttach(FishingRod rod)
    {
        return Game1.player.IsLocalPlayer
            && !rod.inUse()
            && Game1.activeClickableMenu is null
            && Game1.currentMinigame is null
            && !Game1.eventUp
            && !Game1.isFestival();
    }

    private void AttachFromInventory(FishingRod rod, int inventoryIndex, BaitAttachmentAction action)
    {
        if (inventoryIndex < 0 || inventoryIndex >= Game1.player.Items.Count
            || Game1.player.Items[inventoryIndex] is not SObject { Category: SObject.baitCategory } bait)
        {
            return;
        }

        int originalStack = bait.Stack;
        Game1.player.removeItemFromInventory(bait);
        SObject? remainder = rod.attach(bait);
        if (remainder is not null)
            Game1.player.Items[inventoryIndex] = remainder;

        bool changed = remainder is null || remainder.Stack < originalStack;
        if (!changed)
        {
            monitor.Log($"Couldn't attach {bait.DisplayName} to the current fishing rod.", LogLevel.Warn);
            return;
        }

        monitor.Log(
            $"{(action == BaitAttachmentAction.RefillFromInventory ? "Refilled" : "Attached")} " +
            $"{bait.DisplayName} for local screen {Context.ScreenId}.",
            LogLevel.Trace);
    }

    private void SpawnAndAttach(FishingRod rod, string itemId, int amount)
    {
        Item item = ItemRegistry.Create(itemId, amount);
        if (item is not SObject { Category: SObject.baitCategory } bait)
        {
            monitor.Log($"Couldn't spawn configured bait '{itemId}' because it isn't a valid bait item.",
                LogLevel.Error);
            return;
        }

        SObject? remainder = rod.attach(bait);
        if (remainder is not null)
        {
            monitor.Log($"Couldn't attach spawned bait '{itemId}' to the current fishing rod.", LogLevel.Warn);
            return;
        }

        Game1.addHUDMessage(new HUDMessage(
            string.Format(translate("hud.bait.spawned"), bait.DisplayName, bait.Stack)));
        monitor.Log(
            $"Spawned and attached {amount} {bait.DisplayName} for local screen {Context.ScreenId}.",
            LogLevel.Warn);
    }
}
