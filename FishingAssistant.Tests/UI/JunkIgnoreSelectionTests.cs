using FishingAssistant.Configuration;
using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class JunkIgnoreSelectionTests
{
    [Fact]
    public void Toggle_AddsAndRemovesQualifiedIdsCaseInsensitively()
    {
        List<string> selected = [];

        Assert.True(JunkIgnoreSelection.Toggle(selected, "(O)168"));
        Assert.Equal(["(O)168"], selected);

        Assert.False(JunkIgnoreSelection.Toggle(selected, "(o)168"));
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

        Assert.Equal([items[1]], JunkIgnoreSelection.Filter(items, "drift"));
        Assert.Equal([items[0]], JunkIgnoreSelection.Filter(items, "168"));
        Assert.Equal(items, JunkIgnoreSelection.Filter(items, "  "));
    }
}
