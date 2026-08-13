using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class MenuLayoutContextTests
{
    [Fact]
    public void Create_IsEqualForTheSameLayoutInputs()
    {
        MenuLayoutContext first = Create();
        MenuLayoutContext second = Create();

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(1281, 720, 1f, 1f, "en", "first,second")]
    [InlineData(1280, 721, 1f, 1f, "en", "first,second")]
    [InlineData(1280, 720, 1.1f, 1f, "en", "first,second")]
    [InlineData(1280, 720, 1f, 1.1f, "en", "first,second")]
    [InlineData(1280, 720, 1f, 1f, "th", "first,second")]
    [InlineData(1280, 720, 1f, 1f, "en", "first,third")]
    public void Create_ChangesWhenAnyLayoutInputChanges(
        int width,
        int height,
        float uiScale,
        float zoomLevel,
        string language,
        string optionKeys)
    {
        MenuLayoutContext baseline = Create();
        MenuLayoutContext changed = MenuLayoutContext.Create(
            width, height, uiScale, zoomLevel, language, optionKeys.Split(','));

        Assert.NotEqual(baseline, changed);
    }

    private static MenuLayoutContext Create()
    {
        return MenuLayoutContext.Create(1280, 720, 1f, 1f, "en", ["first", "second"]);
    }
}
