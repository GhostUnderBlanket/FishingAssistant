using FishingAssistant.Configuration;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace FishingAssistant.Equipment;

internal sealed class TackleAttachmentService(IMonitor monitor, Func<string, string> translate)
{
    public void UpdateCurrent(ModConfig config)
    {
        if (!Context.IsWorldReady || Game1.player.CurrentTool is not FishingRod rod)
            return;

        List<TackleSlotState> slots = [];
        for (int slot = FishingRod.TackleIndex; slot < rod.AttachmentSlotsCount; slot++)
        {
            string preference = slot == FishingRod.TackleIndex
                ? config.PreferredTackle
                : config.PreferredAdvIridiumTackle;
            slots.Add(new TackleSlotState(slot, rod.attachments[slot] is not null, preference));
        }

        List<TackleInventoryCandidate> candidates = Game1.player.Items
            .Select((item, index) => (item, index))
            .Where(entry => entry.item is SObject { Category: SObject.tackleCategory })
            .Select(entry => new TackleInventoryCandidate(entry.index, entry.item!.QualifiedItemId))
            .ToList();
        TackleAttachmentConditions conditions = new(
            config.AutoAttachTackles,
            this.IsSafeToAttach(rod),
            rod.CanUseTackle(),
            config.SpawnTackleIfDontHave,
            slots,
            candidates
        );
        TackleAttachmentDecision decision = TackleAttachmentPolicy.Decide(conditions);
        switch (decision.Action)
        {
            case TackleAttachmentAction.AttachFromInventory:
                this.AttachFromInventory(rod, decision.TargetSlot, decision.InventoryIndex);
                break;
            case TackleAttachmentAction.Spawn:
                this.SpawnAndAttach(rod, decision.TargetSlot, decision.SpawnItemId!);
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

    private void AttachFromInventory(FishingRod rod, int targetSlot, int inventoryIndex)
    {
        if (!this.IsValidEmptySlot(rod, targetSlot)
            || inventoryIndex < 0
            || inventoryIndex >= Game1.player.Items.Count
            || Game1.player.Items[inventoryIndex] is not SObject { Category: SObject.tackleCategory } tackle)
        {
            return;
        }

        Game1.player.removeItemFromInventory(tackle);
        rod.attachments[targetSlot] = tackle;
        Game1.playSound("button1");
        monitor.Log(
            $"Attached {tackle.DisplayName} to tackle slot {targetSlot} for local screen {Context.ScreenId}.",
            LogLevel.Trace);
    }

    private void SpawnAndAttach(FishingRod rod, int targetSlot, string itemId)
    {
        if (!this.IsValidEmptySlot(rod, targetSlot))
            return;

        Item item = ItemRegistry.Create(itemId);
        if (item is not SObject { Category: SObject.tackleCategory } tackle)
        {
            monitor.Log($"Couldn't spawn configured tackle '{itemId}' because it isn't a valid tackle item.",
                LogLevel.Error);
            return;
        }

        rod.attachments[targetSlot] = tackle;
        Game1.playSound("button1");
        Game1.addHUDMessage(new HUDMessage(
            string.Format(translate("hud.tackle.spawned"), tackle.DisplayName, targetSlot)));
        monitor.Log(
            $"Spawned and attached {tackle.DisplayName} to slot {targetSlot} for local screen " +
            $"{Context.ScreenId}.",
            LogLevel.Warn);
    }

    private bool IsValidEmptySlot(FishingRod rod, int slot)
    {
        return slot >= FishingRod.TackleIndex
            && slot < rod.AttachmentSlotsCount
            && rod.attachments[slot] is null;
    }
}
