using FishingAssistant.HUD;

namespace FishingAssistant.Tests.HUD;

public sealed class AutomationHudVisibilityPolicyTests
{
    private static AutomationHudVisibilityConditions VisibleConditions => new(
        DisplayHud: true,
        HasBlockingMenu: false,
        IsEvent: false,
        IsFestival: false,
        HasUnsupportedMinigame: false);

    [Fact]
    public void ShouldDraw_ShowsDuringOrdinaryWorldPlay()
    {
        Assert.True(AutomationHudVisibilityPolicy.ShouldDraw(VisibleConditions));
    }

    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(true, true, false, false, false)]
    [InlineData(true, false, true, false, false)]
    [InlineData(true, false, false, false, true)]
    public void ShouldDraw_HidesWhenHudWouldObscureUnsupportedContext(
        bool displayHud,
        bool hasBlockingMenu,
        bool isEvent,
        bool isFestival,
        bool hasUnsupportedMinigame)
    {
        AutomationHudVisibilityConditions conditions = new(
            displayHud,
            hasBlockingMenu,
            isEvent,
            isFestival,
            hasUnsupportedMinigame);

        Assert.False(AutomationHudVisibilityPolicy.ShouldDraw(conditions));
    }

    [Fact]
    public void ShouldDraw_AllowsSupportedFishingFestival()
    {
        AutomationHudVisibilityConditions conditions = VisibleConditions with
        {
            IsEvent = true,
            IsFestival = true
        };

        Assert.True(AutomationHudVisibilityPolicy.ShouldDraw(conditions));
    }
}
