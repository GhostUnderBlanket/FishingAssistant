namespace FishingAssistant.Configuration;

internal sealed class ConfigEditSession(ModConfig draft, int baseRevision, string? profileKey = null)
{
    public ModConfig Draft { get; set; } = draft;

    public int BaseRevision { get; } = baseRevision;

    public string? ProfileKey { get; } = profileKey;

    public void EnsureCurrent(int currentRevision)
    {
        if (this.BaseRevision != currentRevision)
        {
            throw new InvalidOperationException(
                "The configuration changed after this menu opened. Close and reopen it before applying."
            );
        }
    }
}
