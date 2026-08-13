using StardewModdingAPI;

namespace FishingAssistant.UI.Controls;

internal sealed class KeybindCaptureGate
{
    public bool IsWaitingForRelease { get; private set; } = true;

    public IReadOnlyList<SButton> Observe(
        IReadOnlyList<SButton> pressed,
        IReadOnlyList<SButton> held)
    {
        ArgumentNullException.ThrowIfNull(pressed);
        ArgumentNullException.ThrowIfNull(held);

        if (this.IsWaitingForRelease)
        {
            if (held.All(button => button == SButton.None))
                this.IsWaitingForRelease = false;
            return [];
        }

        if (pressed.All(button => button == SButton.None))
            return [];

        return held
            .Concat(pressed)
            .Where(button => button != SButton.None)
            .Distinct()
            .ToArray();
    }
}
