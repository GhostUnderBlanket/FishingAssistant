using FishingAssistant.Configuration;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace FishingAssistant.Equipment;

internal sealed class InfiniteAttachmentService(IMonitor monitor)
{
    private readonly PerScreen<PreservationState> screens = new(() => new PreservationState());

    public void UpdateCurrent(ModConfig config)
    {
        PreservationState state = this.screens.Value;
        FishingRod? currentRod = Context.IsWorldReady ? Game1.player.CurrentTool as FishingRod : null;
        bool hasPreservableAttachment = currentRod is not null
            && (config.InfiniteBait && currentRod.GetBait() is not null
                || config.InfiniteTackle && currentRod.GetTackle().Any(tackle => tackle is not null));
        AttachmentPreservationConditions conditions = new(
            state.Snapshot is not null,
            state.Snapshot is not null && ReferenceEquals(state.Snapshot.Rod, currentRod),
            currentRod?.inUse() == true,
            hasPreservableAttachment
        );
        switch (AttachmentPreservationPolicy.Decide(conditions))
        {
            case AttachmentPreservationAction.Capture:
                state.Snapshot = Capture(currentRod!, config);
                break;
            case AttachmentPreservationAction.Restore:
                this.Restore(state);
                break;
        }
    }

    public void RestoreCurrent()
    {
        this.Restore(this.screens.Value);
    }

    public void RestoreAll()
    {
        foreach (PreservationState state in this.screens.GetActiveValues().Select(pair => pair.Value))
            this.Restore(state);
    }

    public void ResetAll()
    {
        this.screens.ResetAllScreens();
    }

    private static AttachmentSnapshot Capture(FishingRod rod, ModConfig config)
    {
        SObject? bait = config.InfiniteBait ? rod.GetBait() : null;
        List<TackleSnapshot> tackle = [];
        if (config.InfiniteTackle)
        {
            for (int slot = FishingRod.TackleIndex; slot < rod.AttachmentSlotsCount; slot++)
            {
                if (rod.attachments[slot] is SObject item)
                    tackle.Add(new TackleSnapshot(slot, item, item.uses.Value));
            }
        }

        return new AttachmentSnapshot(rod, bait, bait?.Stack ?? 0, tackle);
    }

    private void Restore(PreservationState state)
    {
        AttachmentSnapshot? snapshot = state.Snapshot;
        if (snapshot is null)
            return;

        RestoreBait(snapshot);
        foreach (TackleSnapshot tackle in snapshot.Tackle)
            this.RestoreTackle(snapshot.Rod, tackle);

        monitor.Log("Restored consumed bait or tackle from an infinite-attachment snapshot.", LogLevel.Trace);
        state.Snapshot = null;
    }

    private static void RestoreBait(AttachmentSnapshot snapshot)
    {
        if (snapshot.Bait is null || !snapshot.Rod.CanUseBait())
            return;

        SObject? current = snapshot.Rod.attachments[FishingRod.BaitIndex];
        if (current is not null && !ReferenceEquals(current, snapshot.Bait))
            return;

        snapshot.Bait.Stack = snapshot.BaitStack;
        snapshot.Rod.attachments[FishingRod.BaitIndex] = snapshot.Bait;
    }

    private void RestoreTackle(FishingRod rod, TackleSnapshot snapshot)
    {
        if (snapshot.Slot < FishingRod.TackleIndex || snapshot.Slot >= rod.AttachmentSlotsCount)
            return;

        SObject? current = rod.attachments[snapshot.Slot];
        if (current is not null && !ReferenceEquals(current, snapshot.Item))
        {
            monitor.Log(
                $"Couldn't restore infinite tackle in slot {snapshot.Slot} because a different item is attached.",
                LogLevel.Warn);
            return;
        }

        snapshot.Item.uses.Value = snapshot.Uses;
        rod.attachments[snapshot.Slot] = snapshot.Item;
    }

    private sealed class PreservationState
    {
        public AttachmentSnapshot? Snapshot { get; set; }
    }

    private sealed record AttachmentSnapshot(
        FishingRod Rod,
        SObject? Bait,
        int BaitStack,
        IReadOnlyList<TackleSnapshot> Tackle);

    private sealed record TackleSnapshot(int Slot, SObject Item, int Uses);
}
