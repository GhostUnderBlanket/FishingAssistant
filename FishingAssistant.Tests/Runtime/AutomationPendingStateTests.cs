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
        Assert.False(state.HookAttemptedForNibble);
        Assert.False(state.IsPursuingTreasure);
        Assert.Null(state.ConfiguredBobberBar);
        Assert.Equal(0, state.FishPopupVisibleTicks);
        Assert.False(state.FishPopupCloseAttempted);
        Assert.Equal(PendingAutomationAction.None, state.Action);
        Assert.Equal(0, state.ActionTicks);
    }
}
