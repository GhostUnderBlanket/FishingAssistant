using FishingAssistant.Configuration;
using FishingAssistant.HUD;
using Microsoft.Xna.Framework;

namespace FishingAssistant.Tests.HUD;

public sealed class AutomationHudLayoutTests
{
    [Theory]
    [InlineData((int)HudPosition.Left, 142)]
    [InlineData((int)HudPosition.Right, 1042)]
    public void Place_FollowsConfiguredSideOfBottomToolbar(int position, int expectedX)
    {
        Rectangle result = AutomationHudLayout.Place(new(
            1280,
            720,
            (HudPosition)position,
            ToolbarWidth: 896,
            ToolbarOpacity: 1f,
            IsFishingMinigame: false,
            IsFestival: false,
            IsToolbarAtTop: false));

        Assert.Equal(expectedX, result.X);
        Assert.Equal(616, result.Y);
        Assert.Equal(96, result.Width);
    }

    [Fact]
    public void Place_CentersWhenToolbarShouldNotBeFollowed()
    {
        Rectangle result = AutomationHudLayout.Place(new(
            1280,
            720,
            HudPosition.Left,
            ToolbarWidth: 896,
            ToolbarOpacity: 1f,
            IsFishingMinigame: true,
            IsFestival: false,
            IsToolbarAtTop: false));

        Assert.Equal(592, result.X);
    }

    [Fact]
    public void Place_CentersDuringFestivalInsteadOfFollowingToolbar()
    {
        Rectangle result = AutomationHudLayout.Place(new(
            1280,
            720,
            HudPosition.Right,
            ToolbarWidth: 896,
            ToolbarOpacity: 1f,
            IsFishingMinigame: false,
            IsFestival: true,
            IsToolbarAtTop: false));

        Assert.Equal(592, result.X);
        Assert.Equal(616, result.Y);
    }

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1024, 576)]
    [InlineData(819, 432)]
    [InlineData(640, 360)]
    public void Place_RemainsInsideScaledLocalUiViewport(int width, int height)
    {
        Rectangle result = AutomationHudLayout.Place(new(
            width,
            height,
            HudPosition.Left,
            ToolbarWidth: Math.Min(896, width),
            ToolbarOpacity: 1f,
            IsFishingMinigame: false,
            IsFestival: false,
            IsToolbarAtTop: false));

        Assert.InRange(result.Left, 0, width - result.Width);
        Assert.InRange(result.Right, result.Width, width);
        Assert.InRange(result.Top, 0, height - result.Height);
        Assert.InRange(result.Bottom, result.Height, height);
    }

    [Fact]
    public void Place_UsesTopEdgeWhenToolbarMovesAbovePlayer()
    {
        Rectangle result = AutomationHudLayout.Place(new(
            1280,
            720,
            HudPosition.Left,
            ToolbarWidth: 896,
            ToolbarOpacity: 1f,
            IsFishingMinigame: false,
            IsFestival: false,
            IsToolbarAtTop: true));

        Assert.Equal(8, result.Y);
    }

    [Fact]
    public void Place_ClampsInsideSmallSplitScreenViewport()
    {
        Rectangle result = AutomationHudLayout.Place(new(
            80,
            60,
            HudPosition.Right,
            ToolbarWidth: 896,
            ToolbarOpacity: 1f,
            IsFishingMinigame: false,
            IsFestival: false,
            IsToolbarAtTop: false));

        Assert.Equal(new Rectangle(20, 0, 60, 60), result);
    }

    [Fact]
    public void PlaceBadge_KeepsBadgeInsidePanel()
    {
        Rectangle result = AutomationHudLayout.PlaceBadge(new Rectangle(100, 200, 96, 96));

        Assert.Equal(new Rectangle(112, 220, 28, 28), result);
    }

    [Fact]
    public void PlaceBadge_ShrinksForTinyPanel()
    {
        Rectangle panel = new(10, 20, 12, 12);

        Rectangle result = AutomationHudLayout.PlaceBadge(panel);

        Assert.Equal(panel, result);
    }

    [Fact]
    public void PlaceIcon_CentersRodWithinToolbarSizedFootprint()
    {
        Rectangle result = AutomationHudLayout.PlaceIcon(new Rectangle(100, 200, 96, 96));

        Assert.Equal(new Rectangle(120, 220, 56, 56), result);
    }

    [Fact]
    public void PlaceTreasureIcon_KeepsIconInsidePanelNearRod()
    {
        Rectangle panel = new(100, 200, 96, 96);

        Rectangle icon = AutomationHudLayout.PlaceTreasureIcon(panel);

        Assert.Equal(new Rectangle(156, 254, 20, 22), icon);
        Assert.True(panel.Contains(icon));
    }
}
