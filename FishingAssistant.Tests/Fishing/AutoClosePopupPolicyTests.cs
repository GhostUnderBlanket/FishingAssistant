using FishingAssistant.Fishing;
using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Fishing;

public sealed class AutoClosePopupPolicyTests
{
    private static AutoClosePopupConditions SafeConditions => new(
        true, true, AutomationState.CatchResult, true, false, false, false);

    [Fact]
    public void Decide_WaitsUntilPopupDelayExpires()
    {
        Assert.Equal(AutoClosePopupDecision.Wait,
            AutoClosePopupPolicy.Decide(SafeConditions, AutoClosePopupPolicy.DefaultDelayTicks - 1));
        Assert.Equal(AutoClosePopupDecision.Close,
            AutoClosePopupPolicy.Decide(SafeConditions, AutoClosePopupPolicy.DefaultDelayTicks));
    }

    [Fact]
    public void Decide_ResetsWhenPopupIsNoLongerVisible()
    {
        AutoClosePopupConditions conditions = SafeConditions with { IsFishPopupVisible = false };

        Assert.Equal(AutoClosePopupDecision.Reset,
            AutoClosePopupPolicy.Decide(conditions, AutoClosePopupPolicy.DefaultDelayTicks));
    }

    [Theory]
    [InlineData(false, true, (int)AutomationState.CatchResult, false, false, false)]
    [InlineData(true, false, (int)AutomationState.CatchResult, false, false, false)]
    [InlineData(true, true, (int)AutomationState.Minigame, false, false, false)]
    [InlineData(true, true, (int)AutomationState.CatchResult, true, false, false)]
    [InlineData(true, true, (int)AutomationState.CatchResult, false, true, false)]
    [InlineData(true, true, (int)AutomationState.CatchResult, false, false, true)]
    public void Decide_WaitsWhenClosingIsDisabledOrUnsafe(
        bool automationEnabled,
        bool autoClosePopupEnabled,
        int stateValue,
        bool blockingMenu,
        bool festival,
        bool attempted)
    {
        AutoClosePopupConditions conditions = SafeConditions with
        {
            AutomationEnabled = automationEnabled,
            AutoClosePopupEnabled = autoClosePopupEnabled,
            State = (AutomationState)stateValue,
            HasBlockingMenu = blockingMenu,
            IsFestival = festival,
            CloseAlreadyAttempted = attempted
        };

        Assert.Equal(AutoClosePopupDecision.Wait,
            AutoClosePopupPolicy.Decide(conditions, AutoClosePopupPolicy.DefaultDelayTicks));
    }
}
