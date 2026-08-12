namespace FishingAssistant.Runtime;

internal sealed class AutomationSession
{
    public bool IsEnabled { get; private set; } = true;

    public AutomationState State { get; private set; } = AutomationState.Idle;

    public AutomationTransitionReason LastReason { get; private set; } = AutomationTransitionReason.SaveLoaded;

    public AutomationTransition? Observe(FishingObservation observation)
    {
        AutomationState desired = AutomationStateMachine.Classify(observation);
        if (desired == this.State)
            return null;

        bool legal = AutomationStateMachine.IsLegalTransition(this.State, desired);
        return this.Transition(desired,
            legal ? AutomationTransitionReason.Observation : AutomationTransitionReason.Recovered,
            wasRecovery: !legal);
    }

    public AutomationTransition Toggle()
    {
        this.IsEnabled = !this.IsEnabled;
        return this.Transition(
            AutomationState.Idle,
            this.IsEnabled ? AutomationTransitionReason.Enabled : AutomationTransitionReason.Disabled,
            wasRecovery: false);
    }

    public AutomationTransition? Disable(AutomationTransitionReason reason)
    {
        if (!this.IsEnabled)
        {
            this.LastReason = reason;
            return null;
        }

        this.IsEnabled = false;
        return this.Transition(AutomationState.Idle, reason, wasRecovery: false);
    }

    public AutomationTransition? Reset(AutomationTransitionReason reason)
    {
        if (this.State == AutomationState.Idle)
        {
            this.LastReason = reason;
            return null;
        }

        return this.Transition(AutomationState.Idle, reason, wasRecovery: false);
    }

    private AutomationTransition Transition(
        AutomationState state,
        AutomationTransitionReason reason,
        bool wasRecovery)
    {
        AutomationState previous = this.State;
        this.State = state;
        this.LastReason = reason;
        return new AutomationTransition(previous, state, reason, wasRecovery);
    }
}
