using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace FishingAssistant.HUD;

internal sealed class ActivityLogService
{
    private const int MaximumEntries = 50;
    private readonly PerScreen<ScreenState> screens = new(() => new ScreenState());

    public IReadOnlyList<ActivityLogEntry> Current => this.screens.Value.Entries;

    public void Add(string message, Item? subject = null, ActivityLogSeverity severity = ActivityLogSeverity.Information)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        ScreenState screen = this.screens.Value;
        string? itemId = subject?.QualifiedItemId;
        ActivityLogEntry? latest = screen.Entries.LastOrDefault();
        if (latest is not null
            && string.Equals(latest.Message, message, StringComparison.Ordinal)
            && string.Equals(latest.ItemId, itemId, StringComparison.OrdinalIgnoreCase)
            && latest.Severity == severity)
        {
            screen.Entries[^1] = latest with
            {
                Count = latest.Count + 1,
                TimeOfDay = Context.IsWorldReady ? Game1.timeOfDay : latest.TimeOfDay
            };
            return;
        }

        screen.Entries.Add(new ActivityLogEntry(
            message.Trim(),
            itemId,
            Context.IsWorldReady ? Game1.timeOfDay : 0,
            severity,
            1));
        if (screen.Entries.Count > MaximumEntries)
            screen.Entries.RemoveRange(0, screen.Entries.Count - MaximumEntries);
    }

    public void ClearCurrent() => this.screens.Value.Entries.Clear();

    public void ResetCurrent() => this.screens.Value.Reset();

    public void ResetAll() => this.screens.ResetAllScreens();

    private sealed class ScreenState
    {
        public List<ActivityLogEntry> Entries { get; } = [];

        public void Reset() => this.Entries.Clear();
    }
}

internal sealed record ActivityLogEntry(
    string Message,
    string? ItemId,
    int TimeOfDay,
    ActivityLogSeverity Severity,
    int Count);

internal enum ActivityLogSeverity
{
    Information,
    Success,
    Warning,
    Error
}
