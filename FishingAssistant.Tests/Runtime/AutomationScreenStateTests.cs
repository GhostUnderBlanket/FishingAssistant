using FishingAssistant.Fishing;
using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Runtime;

public sealed class AutomationScreenStateTests
{
    [Theory]
    [InlineData((int)AutomationTransitionReason.ToolChanged)]
    [InlineData((int)AutomationTransitionReason.Warped)]
    [InlineData((int)AutomationTransitionReason.DayStarted)]
    [InlineData((int)AutomationTransitionReason.SaveLoaded)]
    [InlineData((int)AutomationTransitionReason.ReturnedToTitle)]
    [InlineData((int)AutomationTransitionReason.Saving)]
    [InlineData((int)AutomationTransitionReason.PeerDisconnected)]
    [InlineData((int)AutomationTransitionReason.MenuInterrupted)]
    public void Cancel_ClearsAllTransientStateAndKeepsAutomationEnabled(int reason)
    {
        AutomationScreenState state = CreatePopulatedState();

        state.Cancel((AutomationTransitionReason)reason, disable: false);

        AssertTransientStateCleared(state);
        Assert.True(state.Session.IsEnabled);
        Assert.Equal((AutomationTransitionReason)reason, state.Session.LastReason);
    }

    [Fact]
    public void Cancel_TimeoutClearsStateAndDisablesAutomation()
    {
        AutomationScreenState state = CreatePopulatedState();

        state.Cancel(AutomationTransitionReason.TimedOut, disable: true);

        AssertTransientStateCleared(state);
        Assert.False(state.Session.IsEnabled);
        Assert.Equal(AutomationTransitionReason.TimedOut, state.Session.LastReason);
    }

    [Fact]
    public void Cancel_RejectsOrdinaryObservationReason()
    {
        AutomationScreenState state = new();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            state.Cancel(AutomationTransitionReason.Observation, disable: false));
    }

    [Fact]
    public void ResetObservedTool_ForgetsPreviousToolObservation()
    {
        AutomationScreenState state = new() { HasObservedTool = true };

        state.ResetObservedTool();

        Assert.False(state.HasObservedTool);
        Assert.Null(state.LastTool);
    }

    private static AutomationScreenState CreatePopulatedState()
    {
        AutomationScreenState state = new()
        {
            TreasureMenuIdentity = new object(),
            TreasureLootElapsedTicks = 9,
            TreasureLootRequiredTicks = 17,
            TreasureCollectionStopped = true
        };
        state.Pending.ReadyTicks = 12;
        state.Pending.AutomaticCastInProgress = true;
        state.Pending.HookAttemptedForNibble = true;
        state.Pending.IsPursuingTreasure = true;
        state.Pending.ConfiguredBobberBar = new object();
        state.Pending.FishPopupVisibleTicks = 8;
        state.Pending.FishPopupCloseAttempted = true;
        state.Pending.Action = PendingAutomationAction.AutomaticCast;
        state.Pending.ActionTicks = 600;
        state.BlockedTreasureItems.Add(null!);
        return state;
    }

    private static void AssertTransientStateCleared(AutomationScreenState state)
    {
        Assert.Equal(0, state.Pending.ReadyTicks);
        Assert.False(state.Pending.AutomaticCastInProgress);
        Assert.False(state.Pending.HookAttemptedForNibble);
        Assert.False(state.Pending.IsPursuingTreasure);
        Assert.Null(state.Pending.ConfiguredBobberBar);
        Assert.Equal(0, state.Pending.FishPopupVisibleTicks);
        Assert.False(state.Pending.FishPopupCloseAttempted);
        Assert.Equal(PendingAutomationAction.None, state.Pending.Action);
        Assert.Equal(0, state.Pending.ActionTicks);
        Assert.Null(state.TreasureMenuIdentity);
        Assert.Equal(0, state.TreasureLootElapsedTicks);
        Assert.Equal(TreasureLootPolicy.InitialDelayTicks, state.TreasureLootRequiredTicks);
        Assert.False(state.TreasureCollectionStopped);
        Assert.Empty(state.BlockedTreasureItems);
    }
}
