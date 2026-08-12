using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Runtime;

public sealed class AutomationStateMachineTests
{
    public static IEnumerable<object[]> Observations =>
    [
        [new FishingObservation(false, true, true), AutomationState.Idle],
        [new FishingObservation(true, true, false), AutomationState.Idle],
        [new FishingObservation(true, true, true), AutomationState.Ready],
        [new FishingObservation(true, true, true, IsTimingCast: true), AutomationState.Casting],
        [new FishingObservation(true, true, true, IsFishing: true), AutomationState.WaitingForBite],
        [new FishingObservation(true, true, true, IsNibbling: true), AutomationState.Hooking],
        [new FishingObservation(true, true, true, IsMinigame: true), AutomationState.Minigame],
        [new FishingObservation(true, true, true, IsFishCaught: true), AutomationState.CatchResult],
        [new FishingObservation(true, true, true, IsTreasureMenu: true), AutomationState.TreasureMenu],
        [new FishingObservation(true, true, true, HasBlockingMenu: true), AutomationState.Paused]
    ];

    [Theory]
    [MemberData(nameof(Observations))]
    public void Classify_ReturnsObservedFishingState(object observationValue, object expectedValue)
    {
        FishingObservation observation = Assert.IsType<FishingObservation>(observationValue);
        AutomationState expected = Assert.IsType<AutomationState>(expectedValue);
        Assert.Equal(expected, AutomationStateMachine.Classify(observation));
    }

    [Fact]
    public void Observe_FollowsLegalFishingSequence()
    {
        AutomationSession session = new();

        AutomationTransition ready = session.Observe(new(true, true, true))!;
        AutomationTransition casting = session.Observe(new(true, true, true, IsCasting: true))!;
        AutomationTransition waiting = session.Observe(new(true, true, true, IsFishing: true))!;
        AutomationTransition hooking = session.Observe(new(true, true, true, IsNibbling: true))!;
        AutomationTransition minigame = session.Observe(new(true, true, true, IsMinigame: true))!;

        Assert.False(ready.WasRecovery);
        Assert.False(casting.WasRecovery);
        Assert.False(waiting.WasRecovery);
        Assert.False(hooking.WasRecovery);
        Assert.False(minigame.WasRecovery);
        Assert.Equal(AutomationState.Minigame, session.State);
    }

    [Fact]
    public void Observe_RecoversWhenGameSkipsExpectedState()
    {
        AutomationSession session = new();
        session.Observe(new(true, true, true));

        AutomationTransition transition = session.Observe(new(true, true, true, IsMinigame: true))!;

        Assert.True(transition.WasRecovery);
        Assert.Equal(AutomationTransitionReason.Recovered, transition.Reason);
        Assert.Equal(AutomationState.Minigame, session.State);
    }

    [Fact]
    public void Toggle_DisablesOnlyCurrentSessionAndReturnsToIdle()
    {
        AutomationSession session = new();
        session.Observe(new(true, true, true));

        AutomationTransition transition = session.Toggle();

        Assert.False(session.IsEnabled);
        Assert.Equal(AutomationState.Idle, session.State);
        Assert.Equal(AutomationTransitionReason.Disabled, transition.Reason);
    }

    [Fact]
    public void ToggleTreasureTargeting_ChangesOnlyRuntimePreference()
    {
        AutomationSession session = new();
        session.Observe(new(true, true, true));

        Assert.True(session.ToggleTreasureTargeting());
        Assert.True(session.IsTreasureTargetingEnabled);
        Assert.Equal(AutomationState.Ready, session.State);
        Assert.False(session.ToggleTreasureTargeting());
        Assert.False(session.IsTreasureTargetingEnabled);
    }

    [Fact]
    public void Disable_StopsEnabledSessionWithExplicitReason()
    {
        AutomationSession session = new();
        session.Observe(new(true, true, true));

        AutomationTransition transition = session.Disable(AutomationTransitionReason.LateNight)!;

        Assert.False(session.IsEnabled);
        Assert.Equal(AutomationState.Idle, session.State);
        Assert.Equal(AutomationTransitionReason.LateNight, transition.Reason);
        Assert.Null(session.Disable(AutomationTransitionReason.LateNight));
    }
}
