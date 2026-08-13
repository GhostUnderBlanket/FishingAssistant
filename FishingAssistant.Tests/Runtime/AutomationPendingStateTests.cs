using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Runtime;

public sealed class AutomationPendingStateTests
{
    [Fact]
    public void Clear_ResetsEveryPendingAutomationFlag()
    {
        AutomationPendingState state = new()
        {
            ReadyTicks = 42,
            AutomaticCastInProgress = true,
            ManualCastPowerTicks = 45,
            PlayerCastInputObserved = true,
            ManualCastWasTiming = true,
            ManualCastPowerUnlocked = true,
            SessionCastPower = 65,
            HookAttemptedForNibble = true,
            IsPursuingTreasure = true,
            ConfiguredBobberBar = new object(),
            FishPopupVisibleTicks = 30,
            FishPopupCloseAttempted = true,
            Action = PendingAutomationAction.AutomaticCast,
            ActionTicks = 600
        };

        state.Clear();

        Assert.Equal(0, state.ReadyTicks);
        Assert.False(state.AutomaticCastInProgress);
        Assert.Equal(0, state.ManualCastPowerTicks);
        Assert.False(state.PlayerCastInputObserved);
        Assert.False(state.ManualCastWasTiming);
        Assert.False(state.ManualCastPowerUnlocked);
        Assert.Null(state.SessionCastPower);
        Assert.False(state.HookAttemptedForNibble);
        Assert.False(state.IsPursuingTreasure);
        Assert.Null(state.ConfiguredBobberBar);
        Assert.Equal(0, state.FishPopupVisibleTicks);
        Assert.False(state.FishPopupCloseAttempted);
        Assert.Equal(PendingAutomationAction.None, state.Action);
        Assert.Equal(0, state.ActionTicks);
    }
}
