using FishingAssistant.HUD;
using Microsoft.Xna.Framework;

namespace FishingAssistant.Tests.HUD;

public sealed class FishPreviewViewportTests
{
    [Fact]
    public void FromViewports_UsesLocalUiSizeAndConvertsGameCoordinates()
    {
        FishPreviewCoordinateSpace result = FishPreviewViewport.FromViewports(
            new Rectangle(4320, 1872, 1024, 1080),
            new Rectangle(4320, 1872, 819, 864));

        Assert.Equal(new Rectangle(0, 0, 819, 864), result.Viewport);
        Assert.Equal(new Rectangle(331, 480, 77, 509),
            result.ToUi(new Rectangle(414, 600, 96, 636)));
    }

    [Fact]
    public void FromViewports_ClampsInvalidDimensions()
    {
        FishPreviewCoordinateSpace result = FishPreviewViewport.FromViewports(
            new Rectangle(0, 0, 0, -5),
            new Rectangle(0, 0, 0, -2));

        Assert.Equal(new Rectangle(0, 0, 1, 1), result.Viewport);
        Assert.Equal(new Rectangle(0, 0, 1, 1), result.ToUi(new Rectangle(0, 0, 0, 0)));
    }
}
