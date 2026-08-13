using FishingAssistant.UI.Controls;
using StardewModdingAPI;

namespace FishingAssistant.Tests.UI;

public sealed class KeybindCaptureGateTests
{
    [Theory]
    [InlineData(SButton.MouseLeft)]
    [InlineData(SButton.ControllerA)]
    [InlineData(SButton.Enter)]
    public void Observe_IgnoresActivationUntilItIsReleased(SButton activation)
    {
        KeybindCaptureGate gate = new();

        Assert.Empty(gate.Observe([activation], [activation]));
        Assert.True(gate.IsWaitingForRelease);
        Assert.Empty(gate.Observe([], []));
        Assert.False(gate.IsWaitingForRelease);
    }

    [Theory]
    [InlineData(SButton.MouseRight)]
    [InlineData(SButton.ControllerX)]
    [InlineData(SButton.F7)]
    public void Observe_CapturesNextInputAfterRelease(SButton input)
    {
        KeybindCaptureGate gate = new();
        gate.Observe([SButton.MouseLeft], [SButton.MouseLeft]);
        gate.Observe([], []);

        Assert.Equal([input], gate.Observe([input], [input]));
    }

    [Fact]
    public void Observe_PreservesHeldChordButtons()
    {
        KeybindCaptureGate gate = new();
        gate.Observe([], []);

        Assert.Equal(
            [SButton.LeftControl, SButton.F7],
            gate.Observe([SButton.F7], [SButton.LeftControl]));
    }
}
