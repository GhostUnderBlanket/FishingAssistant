using FishingAssistant.HUD;
using Microsoft.Xna.Framework;

namespace FishingAssistant.Tests.HUD;

public sealed class FishPreviewViewportTests
{
    [Fact]
    public void FromGameViewport_UsesLocalSizeAndIgnoresCameraPosition()
    {
        Rectangle result = FishPreviewViewport.FromGameViewport(new Rectangle(4320, 1872, 960, 540));

        Assert.Equal(new Rectangle(0, 0, 960, 540), result);
    }

    [Fact]
    public void FromGameViewport_ClampsInvalidDimensions()
    {
        Rectangle result = FishPreviewViewport.FromGameViewport(new Rectangle(0, 0, 0, -5));

        Assert.Equal(new Rectangle(0, 0, 1, 1), result);
    }
}
