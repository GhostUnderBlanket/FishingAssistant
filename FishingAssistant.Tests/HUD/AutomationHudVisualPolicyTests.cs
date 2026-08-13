using FishingAssistant.HUD;
using FishingAssistant.Runtime;
using Microsoft.Xna.Framework;

namespace FishingAssistant.Tests.HUD;

public sealed class AutomationHudVisualPolicyTests
{
    [Fact]
    public void GetVisual_ShowsOrdinaryDisabledState()
    {
        AutomationHudVisual result = AutomationHudVisualPolicy.GetVisual(
            false,
            AutomationState.Idle,
            AutomationTransitionReason.Disabled);

        Assert.Equal(Color.White * 0.2f, result.IconTint);
        Assert.Equal(AutomationHudBadge.Disabled, result.Badge);
    }

    [Fact]
    public void GetVisual_ShowsMenuPause()
    {
        AutomationHudVisual result = AutomationHudVisualPolicy.GetVisual(
            true,
            AutomationState.Paused,
            AutomationTransitionReason.Observation);

        Assert.Equal(Color.Gold, result.IconTint);
        Assert.Equal(AutomationHudBadge.Paused, result.Badge);
    }

    [Theory]
    [InlineData((int)AutomationTransitionReason.LateNight, (int)AutomationHudBadge.LateNight)]
    [InlineData((int)AutomationTransitionReason.LowEnergy, (int)AutomationHudBadge.LowEnergy)]
    [InlineData((int)AutomationTransitionReason.TimedOut, (int)AutomationHudBadge.Warning)]
    public void GetVisual_ShowsSpecificStopReason(int reason, int expectedBadge)
    {
        AutomationHudVisual result = AutomationHudVisualPolicy.GetVisual(
            false,
            AutomationState.Idle,
            (AutomationTransitionReason)reason);

        Assert.Equal((AutomationHudBadge)expectedBadge, result.Badge);
    }

    [Fact]
    public void GetVisual_ShowsRecoveredRuntimeState()
    {
        AutomationHudVisual result = AutomationHudVisualPolicy.GetVisual(
            true,
            AutomationState.Ready,
            AutomationTransitionReason.Recovered);

        Assert.Equal(Color.LightGreen, result.IconTint);
        Assert.Equal(AutomationHudBadge.Recovered, result.Badge);
    }

    [Fact]
    public void GetVisual_ShowsWorkingBadgeForActiveState()
    {
        AutomationHudVisual result = AutomationHudVisualPolicy.GetVisual(
            true,
            AutomationState.Minigame,
            AutomationTransitionReason.Observation);

        Assert.Equal(Color.White, result.IconTint);
        Assert.Equal(AutomationHudBadge.Working, result.Badge);
    }

    [Fact]
    public void GetVisual_HidesBadgeForIdleState()
    {
        AutomationHudVisual result = AutomationHudVisualPolicy.GetVisual(
            true,
            AutomationState.Idle,
            AutomationTransitionReason.Observation);

        Assert.Equal(Color.White, result.IconTint);
        Assert.Equal(AutomationHudBadge.None, result.Badge);
    }
}
