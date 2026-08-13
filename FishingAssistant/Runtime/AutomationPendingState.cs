namespace FishingAssistant.Runtime;

internal sealed class AutomationPendingState
{
    public int ReadyTicks { get; set; }

    public bool AutomaticCastInProgress { get; set; }

    public int ManualCastPowerTicks { get; set; }

    public bool PlayerCastInputObserved { get; set; }

    public bool ManualCastWasTiming { get; set; }

    public bool ManualCastPowerUnlocked { get; set; }

    public int? SessionCastPower { get; set; }

    public bool HookAttemptedForNibble { get; set; }

    public bool IsPursuingTreasure { get; set; }

    public object? ConfiguredBobberBar { get; set; }

    public int FishPopupVisibleTicks { get; set; }

    public bool FishPopupCloseAttempted { get; set; }

    public PendingAutomationAction Action { get; set; }

    public int ActionTicks { get; set; }

    public object? BubbleSteeringRod { get; set; }

    public Microsoft.Xna.Framework.Vector2 BubbleSteeringTarget { get; set; }

    public Microsoft.Xna.Framework.Vector2 BubbleSteeringExpectedPosition { get; set; }

    public void Clear()
    {
        this.ReadyTicks = 0;
        this.AutomaticCastInProgress = false;
        this.ManualCastPowerTicks = 0;
        this.PlayerCastInputObserved = false;
        this.ManualCastWasTiming = false;
        this.ManualCastPowerUnlocked = false;
        this.SessionCastPower = null;
        this.HookAttemptedForNibble = false;
        this.IsPursuingTreasure = false;
        this.ConfiguredBobberBar = null;
        this.FishPopupVisibleTicks = 0;
        this.FishPopupCloseAttempted = false;
        this.Action = PendingAutomationAction.None;
        this.ActionTicks = 0;
        this.BubbleSteeringRod = null;
        this.BubbleSteeringTarget = Microsoft.Xna.Framework.Vector2.Zero;
        this.BubbleSteeringExpectedPosition = Microsoft.Xna.Framework.Vector2.Zero;
    }
}
