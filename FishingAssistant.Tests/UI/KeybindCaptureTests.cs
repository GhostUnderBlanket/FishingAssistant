using FishingAssistant.UI.Controls;
using StardewModdingAPI;

namespace FishingAssistant.Tests.UI;

public sealed class KeybindCaptureTests
{
    [Theory]
    [InlineData(SButton.Escape)]
    [InlineData(SButton.ControllerB)]
    public void Resolve_CancelsWithoutChangingBinding(SButton button)
    {
        KeybindCaptureResult result = KeybindCapture.Resolve([button]);

        Assert.Equal(KeybindCaptureAction.Cancel, result.Action);
        Assert.Empty(result.Buttons);
    }

    [Theory]
    [InlineData(SButton.Back)]
    [InlineData(SButton.Delete)]
    public void Resolve_ClearsBinding(SButton button)
    {
        KeybindCaptureResult result = KeybindCapture.Resolve([button]);

        Assert.Equal(KeybindCaptureAction.Clear, result.Action);
        Assert.Empty(result.Buttons);
    }

    [Fact]
    public void Resolve_PreservesDistinctMultiButtonChord()
    {
        KeybindCaptureResult result = KeybindCapture.Resolve(
            [SButton.LeftControl, SButton.F7, SButton.LeftControl]);

        Assert.Equal(KeybindCaptureAction.Set, result.Action);
        Assert.Equal([SButton.LeftControl, SButton.F7], result.Buttons);
    }
}
