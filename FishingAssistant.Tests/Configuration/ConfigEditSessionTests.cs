using FishingAssistant.Configuration;

namespace FishingAssistant.Tests.Configuration;

public sealed class ConfigEditSessionTests
{
    [Fact]
    public void EnsureCurrent_AcceptsMatchingRevision()
    {
        ConfigEditSession session = new(new ModConfig(), baseRevision: 4);

        session.EnsureCurrent(currentRevision: 4);
    }

    [Fact]
    public void EnsureCurrent_RejectsStaleDraft()
    {
        ConfigEditSession session = new(new ModConfig(), baseRevision: 4);

        Assert.Throws<InvalidOperationException>(() => session.EnsureCurrent(currentRevision: 5));
    }
}
