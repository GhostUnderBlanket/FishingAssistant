using FishingAssistant.Configuration;
using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class JunkListSelectionTests
{
    [Fact]
    public void Toggle_MovesItemBetweenJunkIgnoreAndNormal()
    {
        List<string> junk = [];
        List<string> ignored = [];

        Assert.Equal(JunkItemState.Junk,
            JunkListSelection.Toggle(junk, ignored, "(O)168", JunkListMode.Junk));
        Assert.Equal(["(O)168"], junk);
        Assert.Empty(ignored);

        Assert.Equal(JunkItemState.Ignore,
            JunkListSelection.Toggle(junk, ignored, "(o)168", JunkListMode.Ignore));
        Assert.Empty(junk);
        Assert.Equal(["(o)168"], ignored);

        Assert.Equal(JunkItemState.Normal,
            JunkListSelection.Toggle(junk, ignored, "(O)168", JunkListMode.Ignore));
        Assert.Empty(junk);
        Assert.Empty(ignored);
    }

    [Fact]
    public void Filter_MatchesLocalizedNameOrQualifiedId()
    {
        ConfigItem[] items =
        [
            new("(O)168", ConfigItemKind.Other, "Trash"),
            new("(O)169", ConfigItemKind.Other, "Driftwood")
        ];

        Assert.Equal([items[1]], JunkListSelection.Filter(items, "drift"));
        Assert.Equal([items[0]], JunkListSelection.Filter(items, "168"));
        Assert.Equal(items, JunkListSelection.Filter(items, "  "));
    }
}
