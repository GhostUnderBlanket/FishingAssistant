using FishingAssistant.Configuration;
using FishingAssistant.Fishing;
using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Runtime;

public sealed class AutomationDisabledWorkflowTests
{
    [Fact]
    public void Policies_DoNotIssueCoreAutomaticActionsAfterSessionIsDisabled()
    {
        AutomationScreenState screen = new();
        screen.Session.Observe(new(true, true, true));
        screen.Toggle();

        Assert.Equal(AutoCastDecision.Reset, AutoCastPolicy.Decide(new(
            screen.Session.IsEnabled, true, AutomationState.Ready, true, true, false, true,
            false, false, true), readyTicks: 60, requiredReadyTicks: 60));
        Assert.Equal(AutoHookDecision.Wait, AutoHookPolicy.Decide(new(
            screen.Session.IsEnabled, true, AutomationState.Hooking, true, false, false,
            false, false, false, true)));
        Assert.False(MinigameControlPolicy.Decide(new(
            screen.Session.IsEnabled, true, AutomationState.Minigame, true,
            200f, 100f, 96)).ShouldControl);
        Assert.Equal(AutoClosePopupDecision.Wait, AutoClosePopupPolicy.Decide(new(
            screen.Session.IsEnabled, true, AutomationState.CatchResult, true,
            false, false, false), visibleTicks: AutoClosePopupPolicy.DefaultDelayTicks));
        Assert.Equal(TreasureLootDecision.Wait, TreasureLootPolicy.Decide(new(
            screen.Session.IsEnabled, true, true, false, false, true, true,
            false, false, InventoryFullAction.Stop, IgnoredTreasureAction.KeepOpen),
            elapsedTicks: TreasureLootPolicy.InitialDelayTicks,
            requiredTicks: TreasureLootPolicy.InitialDelayTicks));
    }
}
