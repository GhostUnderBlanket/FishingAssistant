using FishingAssistant.Fishing;
using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Fishing;

public sealed class LowEnergyStopPolicyTests
{
    private static LowEnergyStopConditions SafeConditions => new(
        true, true, AutomationState.Ready, true, false, 20f, 270f, 8f, false, 5);

    [Fact]
    public void Decide_StopsBeforeNextCastWouldExhaustPlayer()
    {
        LowEnergyStopConditions conditions = SafeConditions with { Stamina = 8f };

        Assert.Equal(LowEnergyStopDecision.StopBeforeExhaustion,
            LowEnergyStopPolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_StopsAtEatingThresholdWhenNoFoodWasConsumed()
    {
        LowEnergyStopConditions conditions = SafeConditions with
        {
            AutoEatEnabled = true,
            Stamina = 13.5f
        };

        Assert.Equal(LowEnergyStopDecision.StopAtEatingThreshold,
            LowEnergyStopPolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_WaitsWhileAutomaticEatingIsActive()
    {
        LowEnergyStopConditions conditions = SafeConditions with
        {
            AutoEatEnabled = true,
            Stamina = 5f,
            PlayerIsEating = true
        };

        Assert.Equal(LowEnergyStopDecision.None,
            LowEnergyStopPolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_AllowsCastWithEnergyAboveCost()
    {
        Assert.Equal(LowEnergyStopDecision.None,
            LowEnergyStopPolicy.Decide(SafeConditions));
    }

    [Theory]
    [InlineData(false, true, (int)AutomationState.Ready, true)]
    [InlineData(true, false, (int)AutomationState.Ready, true)]
    [InlineData(true, true, (int)AutomationState.WaitingForBite, true)]
    [InlineData(true, true, (int)AutomationState.Ready, false)]
    public void Decide_DoesNothingOutsideEligibleAutomaticCast(
        bool automationEnabled,
        bool autoCastEnabled,
        int stateValue,
        bool consumesStamina)
    {
        LowEnergyStopConditions conditions = SafeConditions with
        {
            AutomationEnabled = automationEnabled,
            AutoCastEnabled = autoCastEnabled,
            State = (AutomationState)stateValue,
            CastConsumesStamina = consumesStamina,
            Stamina = 0f
        };

        Assert.Equal(LowEnergyStopDecision.None,
            LowEnergyStopPolicy.Decide(conditions));
    }
}
