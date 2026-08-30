namespace FishingAssistant.UI;

internal enum ItemPickerStatusTone
{
    Neutral,
    Warning,
    Positive
}

internal sealed record ItemPickerStatus(
    string Label,
    string Tooltip,
    ItemPickerStatusTone Tone = ItemPickerStatusTone.Neutral);
