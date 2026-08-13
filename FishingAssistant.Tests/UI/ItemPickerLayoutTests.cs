using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class ItemPickerLayoutTests
{
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(640, 360)]
    [InlineData(320, 180)]
    public void Calculate_StaysInsideViewportAndHasUsableGrid(int width, int height)
    {
        ItemPickerLayout layout = ItemPickerLayout.Calculate(width, height);

        Assert.InRange(layout.X, 0, width - 1);
        Assert.InRange(layout.Y, 0, height - 1);
        Assert.True(layout.X + layout.Width <= width);
        Assert.True(layout.Y + layout.Height <= height);
        Assert.InRange(layout.Columns, 1, 5);
        Assert.True(layout.Rows >= 1);
        Assert.True(layout.PageSize >= 1);
        Assert.True(layout.CardWidth >= 1);
        Assert.True(layout.CardHeight >= 1);
        Assert.True(layout.HeaderHeight >= 52);
        Assert.True(layout.SearchHeight >= 52);
        Assert.True(layout.ContentTop > layout.Y + layout.HeaderHeight + 48);
    }
}
