using StardewModdingAPI;

namespace FishingAssistant.UI.Controls;

internal enum KeybindCaptureAction
{
    Cancel,
    Clear,
    Set
}

internal sealed record KeybindCaptureResult(KeybindCaptureAction Action, IReadOnlyList<SButton> Buttons);

internal static class KeybindCapture
{
    public static KeybindCaptureResult Resolve(IReadOnlyList<SButton> buttons)
    {
        ArgumentNullException.ThrowIfNull(buttons);
        if (buttons.Count == 0)
            throw new ArgumentException("At least one button is required.", nameof(buttons));

        if (buttons.Contains(SButton.Escape) || buttons.Contains(SButton.ControllerB))
            return new KeybindCaptureResult(KeybindCaptureAction.Cancel, []);
        if (buttons.Contains(SButton.Back) || buttons.Contains(SButton.Delete))
            return new KeybindCaptureResult(KeybindCaptureAction.Clear, []);

        SButton[] distinct = buttons
            .Where(button => button != SButton.None)
            .Distinct()
            .ToArray();
        return distinct.Length == 0
            ? new KeybindCaptureResult(KeybindCaptureAction.Clear, [])
            : new KeybindCaptureResult(KeybindCaptureAction.Set, distinct);
    }
}
