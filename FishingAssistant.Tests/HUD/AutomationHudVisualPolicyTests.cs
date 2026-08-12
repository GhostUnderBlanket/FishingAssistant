using FishingAssistant.HUD;
using FishingAssistant.Runtime;
using Microsoft.Xna.Framework;

namespace FishingAssistant.Tests.HUD;

public sealed class AutomationHudVisualPolicyTests
{
    [Fact]
    public void GetAutomationTint_DimsDisabledAutomation()
    {
        Assert.Equal(Color.White * 0.2f,
            AutomationHudVisualPolicy.GetAutomationTint(false, AutomationState.Ready));
    }

    [Theory]
    [InlineData((int)AutomationState.Paused)]
    [InlineData((int)AutomationState.Faulted)]
    public void GetAutomationTint_UsesWarningColorForExceptionalState(int state)
    {
        Assert.NotEqual(Color.White,
            AutomationHudVisualPolicy.GetAutomationTint(true, (AutomationState)state));
    }

    [Fact]
    public void GetAutomationTint_UsesNormalColorForActiveState()
    {
        Assert.Equal(Color.White,
            AutomationHudVisualPolicy.GetAutomationTint(true, AutomationState.Minigame));
    }
}
