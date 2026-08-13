using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Runtime;

public sealed class AutomationRecoveryPolicyTests
{
    [Theory]
    [InlineData((int)AutomationState.Casting, true, false, false, (int)PendingAutomationAction.AutomaticCast)]
    [InlineData((int)AutomationState.Hooking, false, true, false, (int)PendingAutomationAction.AutomaticHook)]
    [InlineData((int)AutomationState.CatchResult, false, false, true,
        (int)PendingAutomationAction.CatchPopupClose)]
    [InlineData((int)AutomationState.WaitingForBite, false, false, false, (int)PendingAutomationAction.None)]
    public void GetPendingAction_TracksOnlyModOwnedWork(
        int state,
        bool cast,
        bool hook,
        bool popup,
        int expected)
    {
        PendingAutomationAction result = AutomationRecoveryPolicy.GetPendingAction(
            new AutomationRecoveryConditions((AutomationState)state, cast, hook, popup));

        Assert.Equal((PendingAutomationAction)expected, result);
    }

    [Theory]
    [InlineData((int)PendingAutomationAction.AutomaticCast,
        AutomationRecoveryPolicy.AutomaticCastTimeoutTicks)]
    [InlineData((int)PendingAutomationAction.AutomaticHook,
        AutomationRecoveryPolicy.AutomaticHookTimeoutTicks)]
    [InlineData((int)PendingAutomationAction.CatchPopupClose,
        AutomationRecoveryPolicy.CatchPopupCloseTimeoutTicks)]
    public void HasTimedOut_UsesBoundedActionSpecificLimit(int action, int timeout)
    {
        Assert.False(AutomationRecoveryPolicy.HasTimedOut((PendingAutomationAction)action, timeout - 1));
        Assert.True(AutomationRecoveryPolicy.HasTimedOut((PendingAutomationAction)action, timeout));
    }

    [Fact]
    public void HasTimedOut_NeverTimesOutWhenNoActionIsOwned()
    {
        Assert.False(AutomationRecoveryPolicy.HasTimedOut(PendingAutomationAction.None, int.MaxValue - 1));
    }

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(false, false, false, false)]
    public void ShouldCancelForBlockingMenu_RequiresModOwnedWork(
        bool cast,
        bool hook,
        bool popup,
        bool expected)
    {
        AutomationRecoveryConditions conditions = new(
            AutomationState.Casting,
            cast,
            hook,
            popup);

        Assert.Equal(expected,
            AutomationRecoveryPolicy.ShouldCancelForBlockingMenu(conditions, hasBlockingMenu: true));
        Assert.False(AutomationRecoveryPolicy.ShouldCancelForBlockingMenu(conditions, hasBlockingMenu: false));
    }
}
