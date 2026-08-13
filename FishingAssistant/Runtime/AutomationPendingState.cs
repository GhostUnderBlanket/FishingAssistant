namespace FishingAssistant.Runtime;

internal sealed class AutomationPendingState
{
    public int ReadyTicks { get; set; }

    public bool AutomaticCastInProgress { get; set; }

    public bool HookAttemptedForNibble { get; set; }

    public bool IsPursuingTreasure { get; set; }

    public object? ConfiguredBobberBar { get; set; }

    public int FishPopupVisibleTicks { get; set; }

    public bool FishPopupCloseAttempted { get; set; }

    public PendingAutomationAction Action { get; set; }

    public int ActionTicks { get; set; }

    public void Clear()
    {
        this.ReadyTicks = 0;
        this.AutomaticCastInProgress = false;
        this.HookAttemptedForNibble = false;
        this.IsPursuingTreasure = false;
        this.ConfiguredBobberBar = null;
        this.FishPopupVisibleTicks = 0;
        this.FishPopupCloseAttempted = false;
        this.Action = PendingAutomationAction.None;
        this.ActionTicks = 0;
    }
}
