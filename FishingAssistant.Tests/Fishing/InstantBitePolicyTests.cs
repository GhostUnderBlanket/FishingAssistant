using FishingAssistant.Fishing;
namespace FishingAssistant.Tests.Fishing;

public sealed class InstantBitePolicyTests
{
    private static InstantBiteConditions SafeConditions => new(
        true, true, false, true, false, false, false);

    [Fact]
    public void Decide_TriggersForSafePendingBite()
    {
        Assert.Equal(InstantBiteDecision.Trigger, InstantBitePolicy.Decide(SafeConditions));
    }

    [Theory]
    [InlineData(false, true, false, true, false)]
    [InlineData(true, false, false, true, false)]
    [InlineData(true, true, true, true, false)]
    [InlineData(true, true, false, false, false)]
    [InlineData(true, true, false, true, true)]
    public void Decide_WaitsWhenDisabledOrBiteIsNotReady(
        bool enabled,
        bool isFishing,
        bool isNibbling,
        bool hasPendingTimer,
        bool blockingMenu)
    {
        InstantBiteConditions conditions = SafeConditions with
        {
            InstantBiteEnabled = enabled,
            IsFishing = isFishing,
            IsNibbling = isNibbling,
            HasPendingBiteTimer = hasPendingTimer,
            HasBlockingMenu = blockingMenu
        };

        Assert.Equal(InstantBiteDecision.Wait, InstantBitePolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_AllowsSupportedFestivalFishingMinigame()
    {
        InstantBiteConditions conditions = SafeConditions with
        {
            IsFestival = true,
            IsSupportedFishingMinigame = true
        };

        Assert.Equal(InstantBiteDecision.Trigger, InstantBitePolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_BlocksUnrelatedFestivalContext()
    {
        InstantBiteConditions conditions = SafeConditions with { IsFestival = true };

        Assert.Equal(InstantBiteDecision.Wait, InstantBitePolicy.Decide(conditions));
    }
}
