using FishingAssistant.Fishing;
using FishingAssistant.Runtime;

namespace FishingAssistant.Tests.Fishing;

public sealed class MinigameControlPolicyTests
{
    private static MinigameControlConditions ActiveConditions => new(
        true, true, AutomationState.Minigame, true, 200f, 180f, 100);

    [Fact]
    public void Decide_MovesBarUpWhenFishIsAboveCenter()
    {
        MinigameControlConditions conditions = ActiveConditions with { FishPosition = 50f };

        MinigameControlDecision decision = MinigameControlPolicy.Decide(conditions);

        Assert.True(decision.ShouldControl);
        Assert.True(decision.BarSpeed < 0f);
    }

    [Fact]
    public void Decide_MovesBarDownWhenFishIsBelowCenter()
    {
        MinigameControlConditions conditions = ActiveConditions with { FishPosition = 400f };

        MinigameControlDecision decision = MinigameControlPolicy.Decide(conditions);

        Assert.True(decision.ShouldControl);
        Assert.True(decision.BarSpeed > 0f);
    }

    [Fact]
    public void Decide_StopsInsideCenterDeadZone()
    {
        MinigameControlConditions conditions = ActiveConditions with
        {
            FishPosition = 200f,
            BarPosition = 180f,
            BarHeight = 100
        };

        MinigameControlDecision decision = MinigameControlPolicy.Decide(conditions);

        Assert.True(decision.ShouldControl);
        Assert.Equal(0f, decision.BarSpeed);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void Decide_DoesNotControlOutsideActiveAutomatedMinigame(
        bool automationEnabled,
        bool autoPlayEnabled,
        bool isMinigameState,
        bool isActive)
    {
        MinigameControlConditions conditions = ActiveConditions with
        {
            AutomationEnabled = automationEnabled,
            AutoPlayEnabled = autoPlayEnabled,
            State = isMinigameState ? AutomationState.Minigame : AutomationState.WaitingForBite,
            IsActive = isActive
        };

        Assert.False(MinigameControlPolicy.Decide(conditions).ShouldControl);
    }

    [Fact]
    public void Decide_ClampsLargeCorrections()
    {
        MinigameControlConditions conditions = ActiveConditions with
        {
            FishPosition = 548f,
            BarPosition = 0f,
            BarHeight = 20
        };

        Assert.Equal(16f, MinigameControlPolicy.Decide(conditions).BarSpeed);
    }
}
