using FishingAssistant.Configuration;
using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class JunkListSelectionTests
{
    [Fact]
    public void Toggle_MovesItemBetweenSelectedAndNormal()
    {
        List<string> selected = [];

        Assert.Equal(JunkItemState.Junk,
            JunkListSelection.Toggle(selected, "(O)168", JunkItemState.Junk));
        Assert.Equal(["(O)168"], selected);

        Assert.Equal(JunkItemState.Normal,
            JunkListSelection.Toggle(selected, "(o)168", JunkItemState.Junk));
        Assert.Empty(selected);
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
    public void Group_PutsSelectedItemsBeforeNormalItems()
    {
        ConfigItem junk = new("(O)168", ConfigItemKind.Other, "Trash");
        ConfigItem selected = new("(O)169", ConfigItemKind.Other, "Driftwood");
        ConfigItem normal = new("(O)170", ConfigItemKind.Other, "Glasses");

        JunkListGroups groups = JunkListSelection.Group(
            [junk, selected, normal], [junk.QualifiedItemId]);

        Assert.Equal([junk], groups.Selected);
        Assert.Equal([selected, normal], groups.Normal);
    }

    [Fact]
    public void Group_WorksForTreasureIgnoredItems()
    {
        ConfigItem junk = new("(O)168", ConfigItemKind.Other, "Trash");
        ConfigItem ignored = new("(O)169", ConfigItemKind.Other, "Driftwood");
        ConfigItem normal = new("(O)170", ConfigItemKind.Other, "Glasses");

        JunkListGroups groups = JunkListSelection.Group(
            [junk, ignored, normal], [ignored.QualifiedItemId]);

        Assert.Equal([ignored], groups.Selected);
        Assert.Equal([junk, normal], groups.Normal);
    }
}
