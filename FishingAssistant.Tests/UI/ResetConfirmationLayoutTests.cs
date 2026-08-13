using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class ResetConfirmationLayoutTests
{
    [Theory]
    [InlineData(2048, ResetConfirmationLayout.MaximumTextWidth)]
    [InlineData(1280, ResetConfirmationLayout.MaximumTextWidth)]
    [InlineData(640, 496)]
    [InlineData(320, 176)]
    [InlineData(100, 1)]
    public void GetTextWidth_LeavesRoomForDialogChromeAndViewportMargins(
        int viewportWidth,
        int expectedWidth)
    {
        int width = ResetConfirmationLayout.GetTextWidth(viewportWidth);

        Assert.Equal(expectedWidth, width);
        Assert.InRange(width, 1, ResetConfirmationLayout.MaximumTextWidth);
        if (viewportWidth > ResetConfirmationLayout.DialogChromeAndMargins)
            Assert.True(width + ResetConfirmationLayout.DialogChromeAndMargins <= viewportWidth);
    }
}
