using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Runtime;

public sealed class AutomationLifecyclePolicyTests
{
    [Theory]
    [InlineData((int)AutomationTransitionReason.Disabled)]
    [InlineData((int)AutomationTransitionReason.ToolChanged)]
    [InlineData((int)AutomationTransitionReason.Warped)]
    [InlineData((int)AutomationTransitionReason.DayStarted)]
    [InlineData((int)AutomationTransitionReason.SaveLoaded)]
    [InlineData((int)AutomationTransitionReason.ReturnedToTitle)]
    [InlineData((int)AutomationTransitionReason.Saving)]
    [InlineData((int)AutomationTransitionReason.PeerDisconnected)]
    [InlineData((int)AutomationTransitionReason.MenuInterrupted)]
    [InlineData((int)AutomationTransitionReason.TimedOut)]
    public void CancelsPendingWork_ForRecoveryLifecycleReasons(int reason)
    {
        Assert.True(AutomationLifecyclePolicy.CancelsPendingWork((AutomationTransitionReason)reason));
    }

    [Theory]
    [InlineData((int)AutomationTransitionReason.Observation)]
    [InlineData((int)AutomationTransitionReason.Enabled)]
    [InlineData((int)AutomationTransitionReason.LateNight)]
    [InlineData((int)AutomationTransitionReason.LowEnergy)]
    [InlineData((int)AutomationTransitionReason.Recovered)]
    public void CancelsPendingWork_DoesNotTreatOrdinaryTransitionsAsLifecycleCancellation(int reason)
    {
        Assert.False(AutomationLifecyclePolicy.CancelsPendingWork((AutomationTransitionReason)reason));
    }
}
