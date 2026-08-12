using FishingAssistant.Inventory;

namespace FishingAssistant.Tests.Inventory;

public sealed class AutoEatPolicyTests
{
    private static readonly FoodInventoryCandidate FieldSnack = new(
        2, "(O)403", 45, 20, false, false, false, false);

    private static AutoEatConditions SafeConditions => new(
        true, true, true, 10f, 270f, 5, false, [FieldSnack]);

    [Fact]
    public void Decide_SelectsEligibleFoodAtThreshold()
    {
        AutoEatDecision decision = AutoEatPolicy.Decide(SafeConditions);

        Assert.Equal(AutoEatAction.Eat, decision.Action);
        Assert.Equal(FieldSnack.InventoryIndex, decision.InventoryIndex);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void Decide_DoesNothingWhenDisabledOrUnsafe(bool autoEat, bool automation, bool safe)
    {
        AutoEatConditions conditions = SafeConditions with
        {
            AutoEatEnabled = autoEat,
            AutomationEnabled = automation,
            IsSafeToEat = safe
        };

        Assert.Equal(AutoEatAction.None, AutoEatPolicy.Decide(conditions).Action);
    }

    [Fact]
    public void Decide_DoesNotEatAboveThreshold()
    {
        AutoEatConditions conditions = SafeConditions with { Stamina = 14f };

        Assert.Equal(AutoEatAction.None, AutoEatPolicy.Decide(conditions).Action);
    }

    [Fact]
    public void Decide_ExcludesFishUnlessAllowed()
    {
        FoodInventoryCandidate fish = FieldSnack with { InventoryIndex = 4, IsFish = true };
        AutoEatConditions conditions = SafeConditions with { Candidates = [fish] };

        Assert.Equal(AutoEatAction.None, AutoEatPolicy.Decide(conditions).Action);
        Assert.Equal(AutoEatAction.Eat,
            AutoEatPolicy.Decide(conditions with { AllowEatingFish = true }).Action);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Decide_ExcludesProtectedOrFullnessBlockedFood(bool protectedItem, bool blocked)
    {
        FoodInventoryCandidate food = FieldSnack with
        {
            IsQuestOrProgressionItem = protectedItem,
            IsBlockedByFullness = blocked
        };

        Assert.Equal(AutoEatAction.None,
            AutoEatPolicy.Decide(SafeConditions with { Candidates = [food] }).Action);
    }

    [Fact]
    public void Decide_PrefersPlainFoodOverBuffFood()
    {
        FoodInventoryCandidate buffFood = FieldSnack with
        {
            InventoryIndex = 1,
            SalePrice = 1,
            HasBuff = true
        };

        AutoEatDecision decision = AutoEatPolicy.Decide(
            SafeConditions with { Candidates = [buffFood, FieldSnack] });

        Assert.Equal(FieldSnack.InventoryIndex, decision.InventoryIndex);
    }

    [Fact]
    public void Decide_PrefersLowestCostPerUsefulEnergyThenLeastWaste()
    {
        FoodInventoryCandidate expensive = FieldSnack with
        {
            InventoryIndex = 1,
            StaminaRecovery = 100,
            SalePrice = 200
        };
        FoodInventoryCandidate efficient = FieldSnack with
        {
            InventoryIndex = 5,
            StaminaRecovery = 50,
            SalePrice = 10
        };

        AutoEatDecision decision = AutoEatPolicy.Decide(
            SafeConditions with { Candidates = [expensive, efficient] });

        Assert.Equal(efficient.InventoryIndex, decision.InventoryIndex);
    }

    [Fact]
    public void Decide_UsesInventoryOrderAsFinalTieBreaker()
    {
        FoodInventoryCandidate later = FieldSnack with { InventoryIndex = 8 };
        FoodInventoryCandidate earlier = FieldSnack with { InventoryIndex = 3 };

        AutoEatDecision decision = AutoEatPolicy.Decide(
            SafeConditions with { Candidates = [later, earlier] });

        Assert.Equal(earlier.InventoryIndex, decision.InventoryIndex);
    }
}
