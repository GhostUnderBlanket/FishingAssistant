using FishingAssistant.Configuration;
using FishingAssistant.Fishing;

namespace FishingAssistant.Tests.Fishing;

public sealed class CatchResultPolicyTests
{
    [Fact]
    public void Decide_DefaultsPreserveVanillaResult()
    {
        CatchResultDecision decision = CatchResultPolicy.Decide(Conditions());

        Assert.Equal(new CatchResultDecision(42, 1, false, 1, false), decision);
    }

    [Fact]
    public void Decide_AppliesConfiguredNormalFishResult()
    {
        CatchResultDecision decision = CatchResultPolicy.Decide(Conditions(
            preferredCount: 3,
            preferredQuality: FishQualityPreference.Iridium,
            alwaysPerfect: true,
            alwaysMaximumSize: true));

        Assert.Equal(new CatchResultDecision(56, 4, true, 3, true), decision);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Decide_PreservesSpecialVanillaFlows(bool festival, bool fishPond)
    {
        CatchResultDecision decision = CatchResultPolicy.Decide(Conditions(
            preferredCount: 3,
            preferredQuality: FishQualityPreference.Iridium,
            alwaysPerfect: true,
            alwaysMaximumSize: true,
            festival: festival,
            fishPond: fishPond));

        Assert.Equal(new CatchResultDecision(42, 1, false, 1, false), decision);
    }

    [Theory]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(false, false, 2)]
    public void Decide_PreservesRestrictedOrExistingMultiCatch(
        bool bossFish,
        bool challengeBait,
        int vanillaCount)
    {
        CatchResultDecision decision = CatchResultPolicy.Decide(Conditions(
            vanillaCount: vanillaCount,
            preferredCount: 3,
            bossFish: bossFish,
            challengeBait: challengeBait));

        Assert.Equal(vanillaCount, decision.FishCount);
    }

    [Fact]
    public void Decide_DoesNotApplyFishModifiersToTrash()
    {
        CatchResultDecision decision = CatchResultPolicy.Decide(Conditions(
            preferredCount: 3,
            preferredQuality: FishQualityPreference.Gold,
            alwaysPerfect: true,
            alwaysMaximumSize: true,
            isFish: false));

        Assert.Equal(new CatchResultDecision(42, 1, false, 1, false), decision);
    }

    [Fact]
    public void Decide_ClampsPreferredCountAtCompatibilityBoundary()
    {
        Assert.Equal(3, CatchResultPolicy.Decide(Conditions(preferredCount: 99)).FishCount);
        Assert.Equal(1, CatchResultPolicy.Decide(Conditions(preferredCount: -5)).FishCount);
    }

    [Theory]
    [InlineData(55, 56)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void GetLargestFishSize_MatchesVanillaFullRoll(int maximumFishSize, int expected)
    {
        Assert.Equal(expected, CatchResultPolicy.GetLargestFishSize(maximumFishSize));
    }

    private static CatchResultConditions Conditions(
        int vanillaCount = 1,
        int preferredCount = 1,
        FishQualityPreference preferredQuality = FishQualityPreference.Any,
        bool alwaysPerfect = false,
        bool alwaysMaximumSize = false,
        bool isFish = true,
        bool festival = false,
        bool fishPond = false,
        bool bossFish = false,
        bool challengeBait = false)
    {
        return new CatchResultConditions(
            VanillaFishSize: 42,
            MaximumFishSize: 55,
            VanillaFishQuality: 1,
            VanillaPerfect: false,
            VanillaFishCount: vanillaCount,
            PreferredFishCount: preferredCount,
            PreferredFishQuality: preferredQuality,
            AlwaysPerfect: alwaysPerfect,
            AlwaysMaximumFishSize: alwaysMaximumSize,
            IsFish: isFish,
            IsFestivalFishing: festival,
            IsFromFishPond: fishPond,
            IsBossFish: bossFish,
            UsesChallengeBait: challengeBait);
    }
}
