using FishingAssistant.Fishing;
using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Fishing;

public sealed class AutoHookPolicyTests
{
    private static AutoHookConditions SafeConditions => new(
        true, true, AutomationState.Hooking, true, false, false, false, false, true);

    [Fact]
    public void Decide_HooksOneSafeNibble()
    {
        Assert.Equal(AutoHookDecision.Hook, AutoHookPolicy.Decide(SafeConditions));
    }

    [Fact]
    public void Decide_WaitsAfterHookWasAlreadyAttempted()
    {
        AutoHookConditions conditions = SafeConditions with { HookAlreadyAttempted = true };

        Assert.Equal(AutoHookDecision.Wait, AutoHookPolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_ResetsAttemptAfterNibbleEnds()
    {
        AutoHookConditions conditions = SafeConditions with
        {
            IsNibbling = false,
            HookAlreadyAttempted = true
        };

        Assert.Equal(AutoHookDecision.ResetAttempt, AutoHookPolicy.Decide(conditions));
    }

    [Theory]
    [InlineData(false, true, false, false, false, true)]
    [InlineData(true, false, false, false, false, true)]
    [InlineData(true, true, true, false, false, true)]
    [InlineData(true, true, false, true, false, true)]
    [InlineData(true, true, false, false, true, true)]
    [InlineData(true, true, false, false, false, false)]
    public void Decide_WaitsWhenHookingIsDisabledOrUnsafe(
        bool automationEnabled,
        bool autoHookEnabled,
        bool enchanted,
        bool blockingMenu,
        bool festival,
        bool hookSafe)
    {
        AutoHookConditions conditions = SafeConditions with
        {
            AutomationEnabled = automationEnabled,
            AutoHookEnabled = autoHookEnabled,
            HasAutoHookEnchantment = enchanted,
            HasBlockingMenu = blockingMenu,
            IsFestival = festival,
            IsHookSafe = hookSafe
        };

        Assert.Equal(AutoHookDecision.Wait, AutoHookPolicy.Decide(conditions));
    }
}
