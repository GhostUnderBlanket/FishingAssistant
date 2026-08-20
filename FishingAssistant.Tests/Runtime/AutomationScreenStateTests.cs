using FishingAssistant.Configuration;
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

    [Theory]
    [InlineData((int)AutomationState.Ready)]
    [InlineData((int)AutomationState.Casting)]
    [InlineData((int)AutomationState.WaitingForBite)]
    [InlineData((int)AutomationState.Hooking)]
    [InlineData((int)AutomationState.Minigame)]
    [InlineData((int)AutomationState.CatchResult)]
    [InlineData((int)AutomationState.TreasureMenu)]
    public void Toggle_DisablingAtEveryFishingStageClearsTransientWork(int targetState)
    {
        AutomationScreenState state = CreatePopulatedState();
        AdvanceTo(state.Session, (AutomationState)targetState);

        AutomationTransition transition = state.Toggle();

        AssertTransientStateCleared(state);
        Assert.False(state.Session.IsEnabled);
        Assert.Equal(AutomationState.Idle, state.Session.State);
        Assert.Equal(AutomationTransitionReason.Disabled, transition.Reason);
    }

    [Fact]
    public void Toggle_ReenablingStartsFromCleanIdleState()
    {
        AutomationScreenState state = CreatePopulatedState();
        state.Toggle();

        AutomationTransition transition = state.Toggle();

        Assert.True(state.Session.IsEnabled);
        Assert.Equal(AutomationState.Idle, state.Session.State);
        Assert.Equal(AutomationTransitionReason.Enabled, transition.Reason);
        AssertTransientStateCleared(state);
    }

    [Fact]
    public void Toggle_ReenabledSessionCanObserveReadyWithoutRecovery()
    {
        AutomationScreenState state = CreatePopulatedState();
        state.Toggle();
        state.Toggle();

        AutomationTransition transition = state.Session.Observe(new(true, true, true))!;

        Assert.Equal(AutomationState.Ready, transition.Current);
        Assert.Equal(AutomationTransitionReason.Observation, transition.Reason);
        Assert.False(transition.WasRecovery);
    }

    [Fact]
    public void GetTreasureChestIgnoreIds_RefreshesWhenTheActiveProfileChanges()
    {
        AutomationScreenState state = new();
        ModConfig firstProfile = new() { TreasureChestIgnoreList = ["(O)169"] };
        ModConfig secondProfile = new() { TreasureChestIgnoreList = ["(O)170"] };

        IReadOnlySet<string> first = state.GetTreasureChestIgnoreIds(firstProfile);
        Assert.Contains("(O)169", first);

        IReadOnlySet<string> second = state.GetTreasureChestIgnoreIds(secondProfile);

        Assert.DoesNotContain("(O)169", second);
        Assert.Contains("(O)170", second);
    }

    [Fact]
    public void InvalidateTreasureChestIgnoreIds_RefreshesAnAppliedConfig()
    {
        AutomationScreenState state = new();
        ModConfig config = new() { TreasureChestIgnoreList = ["(O)169"] };
        _ = state.GetTreasureChestIgnoreIds(config);
        config.TreasureChestIgnoreList = ["(O)170"];

        state.InvalidateTreasureChestIgnoreIds();
        IReadOnlySet<string> refreshed = state.GetTreasureChestIgnoreIds(config);

        Assert.DoesNotContain("(O)169", refreshed);
        Assert.Contains("(O)170", refreshed);
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

    private static void AdvanceTo(AutomationSession session, AutomationState target)
    {
        session.Observe(new(true, true, true));
        if (target == AutomationState.Ready)
            return;

        session.Observe(new(true, true, true, IsCasting: true));
        if (target == AutomationState.Casting)
            return;

        session.Observe(new(true, true, true, IsFishing: true));
        if (target == AutomationState.WaitingForBite)
            return;

        session.Observe(new(true, true, true, IsNibbling: true));
        if (target == AutomationState.Hooking)
            return;

        session.Observe(new(true, true, true, IsMinigame: true));
        if (target == AutomationState.Minigame)
            return;

        session.Observe(new(true, true, true, IsFishCaught: true));
        if (target == AutomationState.CatchResult)
            return;

        session.Observe(new(true, true, true, IsTreasureMenu: true));
    }
}
