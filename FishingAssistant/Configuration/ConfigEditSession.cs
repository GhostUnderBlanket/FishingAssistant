namespace FishingAssistant.Configuration;

internal sealed class ConfigEditSession(ModConfig draft, int baseRevision)
{
    public ModConfig Draft { get; set; } = draft;

    public int BaseRevision { get; } = baseRevision;

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
