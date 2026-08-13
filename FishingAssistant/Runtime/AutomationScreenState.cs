using FishingAssistant.Fishing;
using StardewValley;

namespace FishingAssistant.Runtime;

internal sealed class AutomationScreenState
{
    public AutomationSession Session { get; } = new();

    public Tool? LastTool { get; set; }

    public bool HasObservedTool { get; set; }

    public AutomationPendingState Pending { get; } = new();

    public object? TreasureMenuIdentity { get; set; }

    public int TreasureLootElapsedTicks { get; set; }

    public int TreasureLootRequiredTicks { get; set; } = TreasureLootPolicy.InitialDelayTicks;

    public bool TreasureCollectionStopped { get; set; }

    public HashSet<Item> BlockedTreasureItems { get; } = new(ReferenceEqualityComparer.Instance);

    public AutomationTransition? Cancel(AutomationTransitionReason reason, bool disable)
    {
        if (!AutomationLifecyclePolicy.CancelsPendingWork(reason))
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Reason doesn't cancel pending work.");

        this.Pending.Clear();
        this.ResetTreasureLoot();
        return disable
            ? this.Session.Disable(reason)
            : this.Session.Reset(reason);
    }

    public AutomationTransition Toggle()
    {
        if (this.Session.IsEnabled)
        {
            this.Pending.Clear();
            this.ResetTreasureLoot();
        }

        return this.Session.Toggle();
    }

    public void ResetObservedTool()
    {
        this.HasObservedTool = false;
        this.LastTool = null;
    }

    public void ResetTreasureLoot()
    {
        this.TreasureMenuIdentity = null;
        this.TreasureLootElapsedTicks = 0;
        this.TreasureLootRequiredTicks = TreasureLootPolicy.InitialDelayTicks;
        this.TreasureCollectionStopped = false;
        this.BlockedTreasureItems.Clear();
    }
}
