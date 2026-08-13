using FishingAssistant.UI;
using Microsoft.Xna.Framework.Input;

namespace FishingAssistant.Tests.UI;

public sealed class CategoryNavigationInputTests
{
    [Theory]
    [InlineData(Buttons.LeftShoulder)]
    [InlineData(Buttons.LeftTrigger)]
    public void GetDirection_MapsPreviousCategoryButtons(Buttons button)
    {
        Assert.Equal(-1, CategoryNavigationInput.GetDirection(button));
    }

    [Theory]
    [InlineData(Buttons.RightShoulder)]
    [InlineData(Buttons.RightTrigger)]
    public void GetDirection_MapsNextCategoryButtons(Buttons button)
    {
        Assert.Equal(1, CategoryNavigationInput.GetDirection(button));
    }

    [Fact]
    public void GetDirection_IgnoresNonCategoryButtons()
    {
        Assert.Null(CategoryNavigationInput.GetDirection(Buttons.A));
    }
}
