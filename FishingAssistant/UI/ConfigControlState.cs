namespace FishingAssistant.UI;

internal sealed record ConfigControlState(bool IsEnabled, string? UnavailableReasonKey = null)
{
    public static ConfigControlState Enabled { get; } = new(true);

    public static ConfigControlState Disabled(string unavailableReasonKey)
    {
        if (string.IsNullOrWhiteSpace(unavailableReasonKey))
            throw new ArgumentException("An unavailable control needs a reason.", nameof(unavailableReasonKey));

        return new ConfigControlState(false, unavailableReasonKey);
    }
}
