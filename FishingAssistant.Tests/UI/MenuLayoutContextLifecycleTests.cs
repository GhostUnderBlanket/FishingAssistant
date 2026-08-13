using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class MenuLayoutContextLifecycleTests
{
    [Fact]
    public void Create_ProducesStableContextForUnchangedMenu()
    {
        string[] visibleOptions = ["automation", "auto_cast", "auto_hook"];

        MenuLayoutContext first = MenuLayoutContext.Create(
            1280, 720, 1f, 1f, "en", visibleOptions);
        MenuLayoutContext rebuilt = MenuLayoutContext.Create(
            1280, 720, 1f, 1f, "en", visibleOptions);

        Assert.Equal(first, rebuilt);
    }

    [Fact]
    public void Create_ChangesWhenVisibleOptionsChange()
    {
        MenuLayoutContext first = MenuLayoutContext.Create(
            1280, 720, 1f, 1f, "en", ["automation", "auto_cast"]);
        MenuLayoutContext changed = MenuLayoutContext.Create(
            1280, 720, 1f, 1f, "en", ["automation", "auto_cast", "auto_hook"]);

        Assert.NotEqual(first, changed);
    }
}
