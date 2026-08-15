using FishingAssistant.HUD;
using Microsoft.Xna.Framework;

namespace FishingAssistant.Tests.HUD;

public sealed class FishPreviewLayoutTests
{
    [Fact]
    public void Place_UsesRightSideWhenItFits()
    {
        Rectangle result = FishPreviewLayout.Place(
            new Rectangle(0, 0, 1280, 720),
            new Rectangle(500, 100, 96, 636),
            new Point(180, 120));

        Assert.Equal(660, result.X);
        Assert.Equal(100, result.Y);
    }

    [Fact]
    public void Place_UsesLeftSideNearRightEdge()
    {
        Rectangle result = FishPreviewLayout.Place(
            new Rectangle(0, 0, 800, 600),
            new Rectangle(680, 80, 96, 500),
            new Point(180, 120));

        Assert.Equal(436, result.X);
    }

    [Fact]
    public void Place_ForceLeft_UsesLeftSideEvenWhenRightHasMoreSpace()
    {
        Rectangle result = FishPreviewLayout.Place(
            new Rectangle(0, 0, 1280, 720),
            new Rectangle(500, 100, 96, 636),
            new Point(180, 120),
            forceLeft: true);

        Assert.Equal(296, result.X);
        Assert.Equal(100, result.Y);
    }

    [Fact]
    public void Place_ClampsPanelInsideSmallViewport()
    {
        Rectangle viewport = new(0, 0, 220, 140);
        Rectangle result = FishPreviewLayout.Place(
            viewport,
            new Rectangle(80, -20, 96, 300),
            new Point(280, 200));

        Assert.True(viewport.Contains(result));
        Assert.Equal(12, result.X);
        Assert.Equal(12, result.Y);
        Assert.Equal(196, result.Width);
        Assert.Equal(116, result.Height);
    }
}
