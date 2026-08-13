namespace FishingAssistant.Runtime;

internal static class AutomationLifecyclePolicy
{
    public static bool CancelsPendingWork(AutomationTransitionReason reason)
    {
        return reason is AutomationTransitionReason.Disabled
            or AutomationTransitionReason.ToolChanged
            or AutomationTransitionReason.Warped
            or AutomationTransitionReason.DayStarted
            or AutomationTransitionReason.SaveLoaded
            or AutomationTransitionReason.ReturnedToTitle
            or AutomationTransitionReason.Saving
            or AutomationTransitionReason.PeerDisconnected
            or AutomationTransitionReason.MenuInterrupted
            or AutomationTransitionReason.TimedOut;
    }
}
