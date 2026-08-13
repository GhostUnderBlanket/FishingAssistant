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

    [Fact]
    public void GroupForMode_PutsCurrentStateFirstAndExcludesOtherState()
    {
        ConfigItem junk = new("(O)168", ConfigItemKind.Other, "Trash");
        ConfigItem ignored = new("(O)169", ConfigItemKind.Other, "Driftwood");
        ConfigItem normal = new("(O)170", ConfigItemKind.Other, "Glasses");

        JunkListGroups groups = JunkListSelection.GroupForMode(
            [junk, ignored, normal], [junk.QualifiedItemId], [ignored.QualifiedItemId], JunkListMode.Junk);

        Assert.Equal([junk], groups.Selected);
        Assert.Equal([normal], groups.Normal);
    }

    [Fact]
    public void GroupForMode_UsesIgnoredItemsForTreasureIgnoreEditor()
    {
        ConfigItem junk = new("(O)168", ConfigItemKind.Other, "Trash");
        ConfigItem ignored = new("(O)169", ConfigItemKind.Other, "Driftwood");
        ConfigItem normal = new("(O)170", ConfigItemKind.Other, "Glasses");

        JunkListGroups groups = JunkListSelection.GroupForMode(
            [junk, ignored, normal], [junk.QualifiedItemId], [ignored.QualifiedItemId], JunkListMode.Ignore);

        Assert.Equal([ignored], groups.Selected);
        Assert.Equal([normal], groups.Normal);
    }
}
